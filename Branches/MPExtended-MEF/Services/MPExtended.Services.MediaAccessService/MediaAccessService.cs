﻿#region Copyright (C) 2011 MPExtended
// Copyright (C) 2011 MPExtended Developers, http://mpextended.codeplex.com/
// 
// MPExtended is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MPExtended is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MPExtended. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using MPExtended.Libraries.ServiceLib;
using MPExtended.Services.MediaAccessService.Code;
using MPExtended.Services.MediaAccessService.Code.Helper;
using MPExtended.Services.MediaAccessService.Interfaces;
using MPExtended.Services.MediaAccessService.Interfaces.Movie;
using MPExtended.Services.MediaAccessService.Interfaces.Music;
using MPExtended.Services.MediaAccessService.Interfaces.Picture;
using MPExtended.Services.MediaAccessService.Interfaces.Shared;
using MPExtended.Services.MediaAccessService.Interfaces.TVShow;

namespace MPExtended.Services.MediaAccessService
{
    // Here we implement all the methods, but we don't do any data retrieval, that
    // is handled by the backend library classes. We only do some filtering and
    // sorting.

    [ServiceBehavior(IncludeExceptionDetailInFaults = true, InstanceContextMode = InstanceContextMode.Single)]
    public class MediaAccessService : IMediaAccessService
    {
        private const int MOVIE_API = 3;
        private const int MUSIC_API = 3;
        private const int PICTURES_API = 3;
        private const int TVSHOWS_API = 3;
        private const int FILESYSTEM_API = 3;

        [ImportMany]
        private Lazy<IMovieLibrary, IDictionary<string, object>>[] MovieLibraries { get; set; }
        [ImportMany]
        private Lazy<ITVShowLibrary, IDictionary<string, object>>[] TVShowLibraries { get; set; }
        [ImportMany]
        private Lazy<IPictureLibrary, IDictionary<string, object>>[] PictureLibraries { get; set; }
        [ImportMany]
        private Lazy<IMusicLibrary, IDictionary<string, object>>[] MusicLibraries { get; set; }

        private IMovieLibrary ChosenMovieLibrary { get; set; }
        private ITVShowLibrary ChosenTVShowLibrary { get; set; }
        private IPictureLibrary ChosenPictureLibrary { get; set; }
        private IMusicLibrary ChosenMusicLibrary { get; set; }

        public MediaAccessService()
        {
            try
            {
                Compose();
                ChosenMovieLibrary = MovieLibraries.ElementAt(0).Value;
                ChosenMusicLibrary = MusicLibraries.ElementAt(0).Value;
                ChosenPictureLibrary = PictureLibraries.ElementAt(0).Value;
                ChosenTVShowLibrary = TVShowLibraries.ElementAt(0).Value;
            }
            catch (Exception ex)
            {
                Log.Error("Failed to create MEF service", ex);
            }
        }

        private void Compose()
        {
            var catalog = new AggregateCatalog();
            catalog.Catalogs.Add(new DirectoryCatalog(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\MPExtended\Extensions\"));

            var container = new CompositionContainer(catalog);
            container.ComposeParts(this);
        }

        public WebServiceDescription GetServiceDescription()
        {
            return new WebServiceDescription()
            {
                AvailableMovieProvider = MovieLibraries.Select(p => (string)p.Metadata["Database"]).ToList(),
                AvailableMusicProvider = MusicLibraries.Select(p => (string)p.Metadata["Database"]).ToList(),
                AvailablePictureProvider = PictureLibraries.Select(p => (string)p.Metadata["Database"]).ToList(),
                AvailableTvShowProvider = TVShowLibraries.Select(p => (string)p.Metadata["Database"]).ToList(),

                MovieApiVersion = MOVIE_API,
                MusicApiVersion = MUSIC_API,
                PicturesApiVersion = PICTURES_API,
                TvShowsApiVersion = TVSHOWS_API,
                FilesystemApiVersion = FILESYSTEM_API,

                ServiceVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion
            };
        }

        #region Movies
        public WebItemCount GetMovieCount()
        {
            return new WebItemCount() { Count = ChosenMovieLibrary.GetAllMovies().Count };
        }

        public IList<WebMovieBasic> GetAllMoviesBasic(SortMoviesBy sort = SortMoviesBy.Title, OrderBy order = OrderBy.Asc)
        {
            return SortMoviesBasic(ChosenMovieLibrary.GetAllMovies(), sort, order);
        }

        public IList<WebMovieDetailed> GetAllMoviesDetailed(SortMoviesBy sort = SortMoviesBy.Title, OrderBy order = OrderBy.Asc)
        {
            return SortMoviesDetailed(ChosenMovieLibrary.GetAllMoviesDetailed(), sort, order);
        }

        public IList<WebMovieBasic> GetMoviesBasicByRange(int start, int end, SortMoviesBy sort = SortMoviesBy.Title, OrderBy order = OrderBy.Asc)
        {
            return SortMoviesBasic(ChosenMovieLibrary.GetAllMovies(), sort, order).Skip(start).Take(end - start).ToList();
        }

        public IList<WebMovieDetailed> GetMoviesDetailedByRange(int start, int end, SortMoviesBy sort = SortMoviesBy.Title, OrderBy order = OrderBy.Asc)
        {
            return SortMoviesDetailed(ChosenMovieLibrary.GetAllMoviesDetailed(), sort, order).Skip(start).Take(end - start).ToList();
        }

        public IList<WebMovieBasic> GetMoviesBasicByGenre(string genre, SortMoviesBy sort = SortMoviesBy.Title, OrderBy order = OrderBy.Asc)
        {
            return SortMoviesBasic(ChosenMovieLibrary.GetAllMovies().Where(p => p.Genre == genre), sort, order);
        }

        public IList<WebMovieDetailed> GetMoviesDetailedByGenre(string genre, SortMoviesBy sort = SortMoviesBy.Title, OrderBy order = OrderBy.Asc)
        {
            return SortMoviesDetailed(ChosenMovieLibrary.GetAllMoviesDetailed().Where(p => p.Genre == genre), sort, order);
        }

        public IList<WebGenre> GetAllMovieGenres()
        {
            return ChosenMovieLibrary.GetAllGenres();
        }

        public WebMovieDetailed GetMovieDetailedById(string movieId)
        {
            return ChosenMovieLibrary.GetMovieDetailedById(movieId);
        }

        private IList<WebMovieBasic> SortMoviesBasic(IEnumerable<WebMovieBasic> list, SortMoviesBy sort, OrderBy order)
        {
            return list.ToList();
        }

        private IList<WebMovieDetailed> SortMoviesDetailed(IEnumerable<WebMovieDetailed> list, SortMoviesBy sort, OrderBy order)
        {
            return list.ToList();
        }
        #endregion

        #region Music
        public WebItemCount GetMusicTrackCount()
        {
            return new WebItemCount() { Count = ChosenMusicLibrary.GetAllTracks().Count };
        }

        public WebItemCount GetMusicAlbumCount()
        {
            return new WebItemCount() { Count = ChosenMusicLibrary.GetAllAlbums().Count };
        }

        public WebItemCount GetMusicArtistCount()
        {
            return new WebItemCount() { Count = ChosenMusicLibrary.GetAllArtists().Count };
        }

        public IList<WebMusicTrackBasic> GetAllMusicTracksBasic(SortMusicBy sort = SortMusicBy.Title, OrderBy order = OrderBy.Asc)
        {
            return ChosenMusicLibrary.GetAllTracks();
        }

        public IList<WebMusicTrackDetailed> GetAllMusicTracksDetailed(SortMusicBy sort = SortMusicBy.Title, OrderBy order = OrderBy.Asc)
        {
            return ChosenMusicLibrary.GetAllTracksDetailed();
        }

        public IList<WebMusicTrackBasic> GetTracksBasicByRange(int start, int end, SortMusicBy sort = SortMusicBy.Title, OrderBy order = OrderBy.Asc)
        {
            return ChosenMusicLibrary.GetAllTracks().GetRange(start, end).ToList();
        }

        public IList<WebMusicTrackDetailed> GetMusicTracksDetailedByRange(int start, int end, SortMusicBy sort = SortMusicBy.Title, OrderBy order = OrderBy.Asc)
        {
            return ChosenMusicLibrary.GetAllTracksDetailed().GetRange(start, end).ToList();
        }

        public IList<WebMusicTrackBasic> GetMusicTracksBasicByGenre(string genre, SortMusicBy sort = SortMusicBy.Title, OrderBy order = OrderBy.Asc)
        {
            return ChosenMusicLibrary.GetAllTracks().Where(p => p.Genre.Contains(genre)).ToList();
        }

        public IList<WebMusicTrackDetailed> GetMusicTracksDetailedByGenre(string genre, SortMusicBy sort = SortMusicBy.Title, OrderBy order = OrderBy.Asc)
        {
            return ChosenMusicLibrary.GetAllTracksDetailed().Where(p => p.Genre.Contains(genre)).ToList();
        }

        public IList<WebGenre> GetAllMusicGenres()
        {
            return ChosenMusicLibrary.GetAllGenres();
        }

        public WebMusicTrackDetailed GetMusicTrackDetailedById(string Id)
        {
            return ChosenMusicLibrary.GetAllTracksDetailed().Single(p => p.Id == Id);
        }

        public IList<WebMusicAlbumBasic> GetAllMusicAlbumsBasic(SortMusicBy sort = SortMusicBy.Title, OrderBy order = OrderBy.Asc)
        {
            return ChosenMusicLibrary.GetAllAlbums();
        }

        public IList<WebMusicAlbumBasic> GetMusicAlbumsBasicByRange(int start, int end, SortMusicBy sort = SortMusicBy.Title, OrderBy order = OrderBy.Asc)
        {
            return ChosenMusicLibrary.GetAllAlbums().GetRange(start, end).ToList();
        }

        public IList<WebMusicArtistBasic> GetAllMusicArtistsBasic(SortMusicBy sort = SortMusicBy.Title, OrderBy order = OrderBy.Asc)
        {
            return ChosenMusicLibrary.GetAllArtists();
        }

        public IList<WebMusicArtistBasic> GetMusicArtistsBasicByRange(int start, int end, SortMusicBy sort = SortMusicBy.Title, OrderBy order = OrderBy.Asc)
        {
            return ChosenMusicLibrary.GetAllArtists().GetRange(start, end).ToList();
        }

        public WebMusicArtistBasic GetMusicArtistBasicById(string Id)
        {
            return ChosenMusicLibrary.GetAllArtists().Single(p => p.Id == Id);
        }

        public IList<WebMusicTrackBasic> GetMusicTracksBasicByRange(int start, int end, SortMusicBy sort = SortMusicBy.Title, OrderBy order = OrderBy.Asc)
        {
            return ChosenMusicLibrary.GetAllTracks().GetRange(start, end).ToList();
        }

        public IList<WebMusicTrackBasic> GetMusicTracksBasicForAlbum(string id, SortMusicBy sort = SortMusicBy.Title, OrderBy order = OrderBy.Asc)
        {
            return ChosenMusicLibrary.GetAllTracks().Where(p => p.AlbumId == id).ToList();
        }

        public IList<WebMusicTrackDetailed> GetMusicTracksDetailedForAlbum(string id, SortMusicBy sort = SortMusicBy.Title, OrderBy order = OrderBy.Asc)
        {
            return ChosenMusicLibrary.GetAllTracksDetailed().Where(p => p.AlbumId == id).ToList();
        }

        public WebMusicAlbumBasic GetMusicAlbumBasicById(string id)
        {
            return ChosenMusicLibrary.GetAllAlbums().Single(p => p.Id == id);
        }

        public IList<WebMusicAlbumBasic> GetMusicAlbumsBasicForArtist(string id, SortMusicBy sort = SortMusicBy.Title, OrderBy order = OrderBy.Asc)
        {
            return ChosenMusicLibrary.GetAllAlbums().Where(p => p.ArtistId == id).ToList();
        }
        #endregion

        #region Pictures
        public IList<WebPictureBasic> GetAllPicturesBasic(SortPicturesBy sort = SortPicturesBy.Title, OrderBy order = OrderBy.Asc)
        {
            return ChosenPictureLibrary.GetAllPicturesBasic();
        }

        public IList<WebPictureDetailed> GetAllPicturesDetailed(SortPicturesBy sort = SortPicturesBy.Title, OrderBy order = OrderBy.Asc)
        {
            return ChosenPictureLibrary.GetAllPicturesDetailed();
        }

        public WebPictureDetailed GetPictureDetailed(string Id)
        {
            return ChosenPictureLibrary.GetPictureDetailed(Id);
        }

        public IList<WebPictureCategoryBasic> GetAllPictureCategoriesBasic()
        {
            return ChosenPictureLibrary.GetAllPictureCategoriesBasic();
        }

        public WebItemCount GetPictureCount()
        {
            return new WebItemCount() { Count = ChosenPictureLibrary.GetAllPicturesBasic().Count };
        }
        #endregion

        #region TVShows
        public IList<WebTVShowBasic> GetAllTVShowsBasic(SortTVShowsBy sort = SortTVShowsBy.Title, OrderBy order = OrderBy.Asc)
        {
            return ChosenTVShowLibrary.GetAllTVShowsBasic();
        }

        public IList<WebTVShowDetailed> GetAllTVShowsDetailed(SortTVShowsBy sort = SortTVShowsBy.Title, OrderBy order = OrderBy.Asc)
        {
            return ChosenTVShowLibrary.GetAllTVShowsDetailed();
        }

        public IList<WebTVShowBasic> GetTVShowsBasicByRange(int start, int end, SortTVShowsBy sort = SortTVShowsBy.Title, OrderBy order = OrderBy.Asc)
        {
            return SortTVShowList(ChosenTVShowLibrary.GetAllTVShowsBasic(), sort, order).GetRange(start, start - end).ToList();
        }

        public IList<WebTVShowDetailed> GetTVShowsDetailedByRange(int start, int end, SortTVShowsBy sort = SortTVShowsBy.Title, OrderBy order = OrderBy.Asc)
        {
            return SortTVShowList(ChosenTVShowLibrary.GetAllTVShowsDetailed(), sort, order).GetRange(start, start - end).ToList();
        }

        public WebTVShowDetailed GetTVShowDetailed(string id)
        {
            return ChosenTVShowLibrary.GetTVShowDetailed(id);
        }

        public IList<WebTVSeasonBasic> GetAllTVSeasonsBasic(string id, SortTVShowsBy sort = SortTVShowsBy.Title, OrderBy order = OrderBy.Asc)
        {
            return SortTVShowList(ChosenTVShowLibrary.GetAllSeasonsBasic(id), sort, order);
        }

        public IList<WebTVSeasonDetailed> GetAllTVSeasonsDetailed(string id, SortTVShowsBy sort = SortTVShowsBy.Title, OrderBy order = OrderBy.Asc)
        {
            return SortTVShowList(ChosenTVShowLibrary.GetAllSeasonsDetailed(id), sort, order);
        }

        public WebTVSeasonDetailed GetTVSeasonDetailed(string showId, string seasonId, SortTVShowsBy sort = SortTVShowsBy.Title, OrderBy order = OrderBy.Asc)
        {
            return ChosenTVShowLibrary.GetSeasonDetailed(showId, seasonId);
        }

        public IList<WebTVEpisodeBasic> GetAllTVEpisodesBasicForTVShow(string id, SortTVShowsBy sort = SortTVShowsBy.Title, OrderBy order = OrderBy.Asc)
        {
            return SortTVEpisodeList(ChosenTVShowLibrary.GetAllEpisodesBasic().Where(p=> p.ShowId == id).ToList(), id, sort, order);
        }

        public IList<WebTVEpisodeDetailed> GetAllTVEpisodesDetailedForTVShow(string id, SortTVShowsBy sort = SortTVShowsBy.Title, OrderBy order = OrderBy.Asc)
        {
            return SortTVEpisodeList(ChosenTVShowLibrary.GetAllEpisodesDetailed().Where(p => p.ShowId == id).ToList(), id, sort, order);
        }

        public IList<WebTVEpisodeBasic> GetTVEpisodesBasicForTVShowByRange(string id, int start, int end, SortTVShowsBy sort = SortTVShowsBy.Title, OrderBy order = OrderBy.Asc)
        {
            return SortTVEpisodeList(ChosenTVShowLibrary.GetAllEpisodesBasic().Where(p => p.ShowId == id).ToList(), id, sort, order).GetRange(start, end - start).ToList();
        }

        public IList<WebTVEpisodeDetailed> GetTVEpisodesDetailedForTVShowByRange(string id, int start, int end, SortTVShowsBy sort = SortTVShowsBy.Title, OrderBy order = OrderBy.Asc)
        {
            return SortTVEpisodeList(ChosenTVShowLibrary.GetAllEpisodesDetailed().Where(p => p.ShowId == id).ToList(), id, sort, order).GetRange(start, end - start).ToList();
        }

        public IList<WebTVEpisodeBasic> GetTVEpisodesBasicForSeason(string showId, string seasonId, SortTVShowsBy sort = SortTVShowsBy.Title, OrderBy order = OrderBy.Asc)
        {
            return ChosenTVShowLibrary.GetAllEpisodesBasic().Where(p => p.ShowId == showId && p.SeasonId == seasonId).ToList();
        }

        public IList<WebTVEpisodeDetailed> GetTVEpisodesDetailedForSeason(string showId, string seasonId, SortTVShowsBy sort = SortTVShowsBy.Title, OrderBy order = OrderBy.Asc)
        {           
            return ChosenTVShowLibrary.GetAllEpisodesDetailed().Where(p => p.ShowId == showId && p.SeasonId == seasonId).ToList();
        }

        public WebTVEpisodeDetailed GetTVEpisodeDetailed(string id)
        {
            return ChosenTVShowLibrary.GetEpisodeDetailed(id);
        }

        public WebItemCount GetTVEpisodeCount()
        {
            return new WebItemCount() { Count = ChosenTVShowLibrary.GetAllEpisodesBasic().Count };
        }

        public WebItemCount GetTVEpisodeCountForTVShow(string id)
        {
            return new WebItemCount() { Count = ChosenTVShowLibrary.GetAllEpisodesBasic().Where(e => e.ShowId == id).Count() };
        }

        public WebItemCount GetTVShowCount()
        {
            return new WebItemCount() { Count = ChosenTVShowLibrary.GetAllTVShowsBasic().Count };
        }

        public WebItemCount GetTVSeasonCountForTVShow(string id)
        {
            return new WebItemCount() { Count = ChosenTVShowLibrary.GetAllSeasonsBasic(id).Count };
        }

        private IList<T> SortTVShowList<T>(IList<T> list, SortTVShowsBy sort, OrderBy order)
        {
            switch (sort)
            {
                // generic cases
                case SortTVShowsBy.Title:
                    return list.OrderBy(x => ((ITitleSortable)x).Title, order).ToList();
                case SortTVShowsBy.DateAdded:
                    return list.OrderBy(x => ((IDateAddedSortable)x).DateAdded, order).ToList();
                case SortTVShowsBy.Year:
                    return list.OrderBy(x => ((IYearSortable)x).Year, order).ToList();
                case SortTVShowsBy.Genre:
                    return list.OrderBy(x => ((IGenreSortable)x).Genre, order).ToList();
                case SortTVShowsBy.Rating:
                    return list.OrderBy(x => ((IRatingSortable)x).Rating, order).ToList();
                default:
                    return list;
            }
        }

        private IList<T> SortTVEpisodeList<T>(IList<T> list, string showId, SortTVShowsBy sort, OrderBy order) where T : WebTVEpisodeBasic
        {
            switch (sort)
            {
                case SortTVShowsBy.EpisodeNumber:
                    return list.OrderBy(x => ((WebTVEpisodeBasic)x).EpisodeNumber, order).ToList();

                case SortTVShowsBy.SeasonNumberEpisodeNumber:
                    // this can be done better
                    IDictionary<string, WebTVSeasonBasic> seasons = GetAllTVSeasonsBasic(showId).ToDictionary(x => x.Id);
                    return list.Select(x => (WebTVEpisodeBasic)x)
                               .OrderBy(x => seasons[x.SeasonId].SeasonNumber, order)
                               .OrderBy(x => x.EpisodeNumber, order)
                               .Cast<T>()
                               .ToList();

                default:
                    return list;
            }
        }
        #endregion

        public string GetPath(WebMediaType type, string id)
        {
            throw new NotImplementedException();
        }
    }
}
