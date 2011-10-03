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
using System.IO;
using System.Linq;
using MPExtended.Services.MediaAccessService.Interfaces;
using MPExtended.Services.MediaAccessService.Interfaces.Music;
using MPExtended.Services.MediaAccessService.Interfaces.Shared;
using System.Data.SQLite;
using MPExtended.Libraries.SQLitePlugin;

namespace MPExtended.PlugIns.MAS.MPMusic
{
    [Export(typeof(IMusicLibrary))]
    [ExportMetadata("Database", "MPMyMusic")]
    [ExportMetadata("Version", "1.0.0.0")]
    public class MPMusic : Database, IMusicLibrary
    {
        private IPluginData data;

        [ImportingConstructor]
        public MPMusic(IPluginData data)
            : base(data.Configuration["database"])
        {
            this.data = data;
        }

        public IEnumerable<WebMusicTrackBasic> GetAllTracks()
        {
            string sql = "SELECT idTrack, strAlbum, strArtist, strAlbumArtist, iTrack, strTitle, strPath, iDuration, iYear,strGenre " +
                          "FROM tracks " + "WHERE %where %order";
            return new LazyQuery<WebMusicTrackBasic>(this, sql, new List<SQLFieldMapping>() {
                new SQLFieldMapping("", "idTrack", "Id", DataReaders.ReadIntAsString),
                new SQLFieldMapping("", "strArtist", "ArtistId", DataReaders.ReadString),
                 new SQLFieldMapping("", "strAlbum", "AlbumId", DataReaders.ReadString),
                  new SQLFieldMapping("", "strTitle", "Title", DataReaders.ReadString),
                   new SQLFieldMapping("", "iTrack", "TrackNumber", DataReaders.ReadInt32),
                   new SQLFieldMapping("", "strPath", "Path", DataReaders.ReadString),
                    new SQLFieldMapping("", "strGenre", "Genres", DataReaders.ReadString),
                          new SQLFieldMapping("", "iYear", "Year", DataReaders.ReadInt32),
                   new SQLFieldMapping("", "dateAdded", "DateAdded", DataReaders.ReadDateTime)
            });
        }

        public IEnumerable<WebMusicAlbumBasic> GetAllAlbums()
        {
            string sql = "SELECT t.strAlbum, t.strAlbumArtist, t.strArtist, a.iYear, g.strGenre " +
                         "FROM tracks t " +
                         "LEFT JOIN albuminfo a ON t.strAlbum = a.strAlbum " + // this table is empty for me
                         "LEFT JOIN genre g ON a.idGenre = g.strGenre " +
                          "WHERE %where %order";
            return new LazyQuery<WebMusicAlbumBasic>(this, sql, new List<SQLFieldMapping>() {
                new SQLFieldMapping("t", "strAlbum", "Id", DataReaders.ReadString),
                                new SQLFieldMapping("t", "strAlbum", "Title", DataReaders.ReadString),
                new SQLFieldMapping("t", "strAlbumArtist", "AlbumArtist", DataReaders.ReadString),
                 new SQLFieldMapping("t", "strArtist", "Artists", DataReaders.ReadString),
                  new SQLFieldMapping("a", "iYear", "Year", DataReaders.ReadString),
                   new SQLFieldMapping("g", "strGenre", "Genres", DataReaders.ReadString)

            });
        }

        public IEnumerable<WebMusicArtistBasic> GetAllArtists()
        {
            string sql = "SELECT DISTINCT strAlbumArtist FROM tracks WHERE %where GROUP BY strAlbumArtist %order";
            return new LazyQuery<WebMusicArtistBasic>(this, sql, new List<SQLFieldMapping>() {
                new SQLFieldMapping("", "strAlbumArtist", "Id", DataReaders.ReadString),
                new SQLFieldMapping("", "strAlbumArtist", "Title", DataReaders.ReadString)
            });
        }

        public IEnumerable<WebMusicTrackDetailed> GetAllTracksDetailed()
        {
            string sql = "SELECT idTrack, strAlbum, strArtist, strAlbumArtist, iTrack, strTitle, strPath, iDuration, iYear,strGenre " +
                        "FROM tracks " + "WHERE %where %order";
            return new LazyQuery<WebMusicTrackDetailed>(this, sql, new List<SQLFieldMapping>() {
                new SQLFieldMapping("", "idTrack", "Id", DataReaders.ReadIntAsString),
                new SQLFieldMapping("", "strArtist", "ArtistId", DataReaders.ReadString),
                 new SQLFieldMapping("", "strAlbum", "AlbumId", DataReaders.ReadString),
                  new SQLFieldMapping("", "strTitle", "Title", DataReaders.ReadString),
                   new SQLFieldMapping("", "iTrack", "TrackNumber", DataReaders.ReadInt32),
                   new SQLFieldMapping("", "strPath", "Path", DataReaders.ReadString),
                    new SQLFieldMapping("", "strGenre", "Genres", DataReaders.ReadString),
                          new SQLFieldMapping("", "iYear", "Year", DataReaders.ReadInt32),
                   new SQLFieldMapping("", "dateAdded", "DateAdded", DataReaders.ReadDateTime)
            });
        }

        public WebMusicTrackBasic GetTrackBasicById(string trackId)
        {
            throw new NotImplementedException();
        }

        public WebMusicAlbumBasic GetAlbumBasicById(string albumId)
        {
            throw new NotImplementedException();
        }

        public WebMusicArtistBasic GetArtistBasicById(string artistId)
        {
            throw new NotImplementedException();
        }

        public WebMusicTrackDetailed GetTrackDetailedById(string trackId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<WebGenre> GetAllGenres()
        {
            string sql = "SELECT DISTINCT strGenre FROM tracks";
            return ReadList<IEnumerable<string>>(sql, delegate(SQLiteDataReader reader)
            {
                return reader.ReadPipeList(0);
            })
                    .SelectMany(x => x)
                    .Distinct()
                    .OrderBy(x => x)
                    .Select(x => new WebGenre() { Name = x.Replace("| ", "").Replace(" |", "") });
        }

        public IEnumerable<WebCategory> GetAllCategories()
        {
            throw new NotImplementedException();
        }

        public WebFileInfo GetFileInfo(string path)
        {
            return new WebFileInfo(new FileInfo(path));
        }

        public Stream GetFile(string path)
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read);
        }
    }
}
