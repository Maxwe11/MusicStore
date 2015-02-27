﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.Data.Entity;
using Microsoft.Framework.Cache.Memory;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Xunit;
using MusicStore.Models;

namespace MusicStore.Controllers
{
    public class HomeControllerTest
    {
        private readonly IServiceProvider _serviceProvider;

        public HomeControllerTest()
        {
            var services = new ServiceCollection();

            services.AddEntityFramework()
                      .AddInMemoryStore()
                      .AddDbContext<MusicStoreContext>();

            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public void Error_ReturnsErrorView()
        {
            // Arrange
            var controller = new HomeController();
            var errorView = "~/Views/Shared/Error.cshtml";

            // Act
            var result = controller.Error();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.Equal(errorView, viewResult.ViewName);
        }

        [Fact]
        public void Index_GetsSixTopAlbums()
        {
            // Arrange
            var controller = new HomeController()
            {
                DbContext = _serviceProvider.GetRequiredService<MusicStoreContext>(),
                Cache = _serviceProvider.GetRequiredService<IMemoryCache>(),
            };

            PopulateData(controller.DbContext);

            // Action
            var result = controller.Index().Result;

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.NotNull(viewResult.ViewData);
            Assert.NotNull(viewResult.ViewData.Model);

            var albums = Assert.IsType<List<Album>>(viewResult.ViewData.Model);
            Assert.Equal(6, albums.Count);
        }

        [Fact]
        public void StatusCodePage_ReturnsStatusCodePage()
        {
            // Arrange
            var controller = new HomeController();
            var statusCodeView = "~/Views/Shared/StatusCodePage.cshtml";

            // Action
            var result = controller.StatusCodePage();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.Equal(statusCodeView, viewResult.ViewName);
        }

        private void PopulateData(DbContext context)
        {
            var albums = TestAlbumDataProvider.GetAlbums();

            foreach (var album in albums)
            {
                context.Add(album);
            }

            context.SaveChanges();
        }

        private class TestAlbumDataProvider
        {
            public static Album[] GetAlbums()
            {
                var generes = Enumerable.Range(1, 10).Select(n =>
                    new Genre()
                    {
                        GenreId = n,
                        Name = "Genre Name " + n,
                    }).ToArray();

                var artists = Enumerable.Range(1, 10).Select(n =>
                    new Artist()
                    {
                        ArtistId = n + 1,
                        Name = "Artist Name " + n,
                    }).ToArray();

                var albums = Enumerable.Range(1, 10).Select(n =>
                    new Album()
                    {
                        Artist = artists[n - 1],
                        ArtistId = n,
                        Genre = generes[n - 1],
                        GenreId = n,
                    }).ToArray();

                return albums;
            }
        }
    }
}