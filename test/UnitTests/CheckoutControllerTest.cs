﻿using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Core;
using Microsoft.AspNet.Http.Core.Collections;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Xunit;
using MusicStore.Models;

namespace MusicStore.Controllers 
{
    public class CheckoutControllerTest
    {
        private readonly IServiceProvider _serviceProvider;

        public CheckoutControllerTest()
        {
            var services = new ServiceCollection();

            services.AddEntityFramework()
                      .AddInMemoryStore()
                      .AddDbContext<MusicStoreContext>();

            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public void AddressAndPayment_ReturnsDefaultView()
        {
            // Arrange
            var controller = new CheckoutController();

            // Act
            var result = controller.AddressAndPayment();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName);
        }

        [Fact]
        public async Task AddressAndPayment_ReturnsOrderIfInvalidPromoCode()
        {
            // Arrange
            var context = new DefaultHttpContext();

            // AddressAndPayment action reads the Promo code from FormCollection.
            context.Request.Form =
                new FormCollection(new Dictionary<string, string[]>());

            var controller = new CheckoutController()
            {
                ActionContext = new ActionContext(
                    context,
                    new RouteData(),
                    new ActionDescriptor()),
            };

            // Do not need actual data for Order; the Order object will be checked for the reference equality.
            var order = new Order();

            // Act
            var result = await controller.AddressAndPayment(order, new CancellationToken(false));

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.NotNull(viewResult.ViewData);
            Assert.Same(order, viewResult.ViewData.Model);
        }

        [Fact]
        public async Task AddressAndPayment_ReturnsOrderIfRequestCanceled()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Form =
                new FormCollection(new Dictionary<string, string[]>());

            var controller = new CheckoutController()
            {
                ActionContext = new ActionContext(
                    context,
                    new RouteData(),
                    new ActionDescriptor()),
            };

            var order = new Order();

            // Act
            var result = await controller.AddressAndPayment(order, new CancellationToken(true));

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.NotNull(viewResult.ViewData);
            Assert.Same(order, viewResult.ViewData.Model);
        }

        [Fact]
        public async Task AddressAndPayment_ReturnsOrderIfInvalidOrderModel()
        {
            // Arrange
            var controller = new CheckoutController();
            controller.ModelState.AddModelError("a", "ModelErrorA");

            var order = new Order();

            // Act
            var result = await controller.AddressAndPayment(order, new CancellationToken(false));

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.ViewData);
            Assert.Same(order, viewResult.ViewData.Model);
        }

        [Fact]
        public async Task Complete_ReturnsOrderIdIfValid()
        {
            // Arrange
            var orderId = 100;
            var userName = "TestUserA";
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, userName) };

            var httpContext = new DefaultHttpContext()
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims)),
            };

            var dbContext =
                _serviceProvider.GetRequiredService<MusicStoreContext>();
            dbContext.Add(new Order()
            {
                OrderId = orderId,
                Username = userName
            });
            dbContext.SaveChanges();

            var controller = new CheckoutController()
            {
                ActionContext = new ActionContext(
                    httpContext,
                    new RouteData(),
                    new ActionDescriptor()),
                DbContext = dbContext,
            };

            // Act
            var result = await controller.Complete(orderId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.NotNull(viewResult.ViewData);
            Assert.Equal(orderId, viewResult.ViewData.Model);
        }

        [Fact]
        public async Task Complete_ReturnsErrorIfInvalidOrder()
        {
            // Arrange
            var dbContext =
                _serviceProvider.GetRequiredService<MusicStoreContext>();

            var controller = new CheckoutController()
            {
                ActionContext = new ActionContext(
                    new DefaultHttpContext(),
                    new RouteData(),
                    new ActionDescriptor()),
                DbContext = dbContext,
            };

            // Act
            var result = await controller.Complete(100);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.Equal("Error", viewResult.ViewName);
        }
    }
}