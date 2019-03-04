﻿using BookingApp.Controllers;
using BookingApp.Data.Models;
using BookingApp.DTOs;
using BookingApp.Helpers;
using BookingApp.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace BookingAppTests.Controllers
{
    public class AuthControllerTest
    {
        private readonly Mock<INotificationService> mockNotificationService;
        private readonly Mock<IUserService> mockUserService;
        private readonly Mock<IJwtService> mockJwtService;
        private readonly Mock<ApplicationUser> mockUser;
        private readonly Mock<AuthLoginDto> mockAuthLoginDto;
        private readonly Mock<AuthRegisterDto> mockAuthRegisterDto; 

        public AuthControllerTest()
        {
            mockNotificationService = new Mock<INotificationService>();
            mockUserService = new Mock<IUserService>();
            mockJwtService = new Mock<IJwtService>();
            mockUser = new Mock<ApplicationUser>();
            mockAuthLoginDto = new Mock<AuthLoginDto>();
            mockAuthRegisterDto = new Mock<AuthRegisterDto>();
        }

        [Theory]
        [InlineData("token1", "token1")]
        [InlineData("token2", "token2")]
        [InlineData("token1", "token2")]
        [InlineData("token2", "token1")]
        [InlineData("somedata", "somedata")]
        public async Task LoginWithCorrectParametersReturnsJwtTokensAsync(string accessToken, string refreshToken)
        {
            var userClaims = It.IsAny<Claim[]>();
            var expectedAccessToken = accessToken;
            var expectedRefreshToken = refreshToken;
            var expectedTime = DateTime.Now.AddMinutes(120);
            var expectedTokens = new AuthTokensDto
            {
                AccessToken = expectedAccessToken,
                RefreshToken = expectedRefreshToken,
                ExpireOn = expectedTime
            };
            mockUser.Setup(user => user.ApprovalStatus).Returns(true);
            mockUser.Setup(user => user.IsBlocked).Returns(false);
            mockUserService.Setup(userService => userService.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(mockUser.Object);
            mockUserService.Setup(userService => userService.CheckPassword(mockUser.Object, It.IsAny<string>())).ReturnsAsync(true);
            mockJwtService.Setup(jwtService => jwtService.GetClaimsAsync(mockUser.Object)).ReturnsAsync(userClaims);
            mockJwtService.Setup(jwtService => jwtService.GenerateJwtAccessToken(userClaims)).Returns(expectedAccessToken);
            mockJwtService.Setup(jwtService => jwtService.GenerateJwtRefreshToken()).Returns(expectedRefreshToken);
            mockJwtService.Setup(jwtService => jwtService.LoginByRefreshTokenAsync(Guid.NewGuid().ToString(), expectedRefreshToken));
            mockJwtService.Setup(jwtService => jwtService.ExpirationTime).Returns(expectedTime);

            AuthController authController = new AuthController(mockNotificationService.Object, mockUserService.Object, mockJwtService.Object);
            var result = await authController.Login(mockAuthLoginDto.Object);
            
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualTokens = Assert.IsAssignableFrom<AuthTokensDto>(okResult.Value);
            Assert.Equal(expectedTokens, actualTokens);
        }

        [Fact]
        public async Task LoginReturnsBadRequestWhenUserCannotBeFound()
        {
            mockUserService.Setup(userService => userService.GetUserByEmail(It.IsAny<string>())).Throws(new NullReferenceException());

            AuthController authController = new AuthController(mockNotificationService.Object, mockUserService.Object, mockJwtService.Object);
            
            await Assert.ThrowsAsync<NullReferenceException>(() => authController.Login(mockAuthLoginDto.Object));
            mockUserService.Verify(userService => userService.CheckPassword(mockUser.Object, It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task LoginReturnsBadRequestWhenPasswordNotMatchAsync()
        {
            mockUserService.Setup(userService => userService.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(mockUser.Object);
            mockUserService.Setup(userService => userService.CheckPassword(mockUser.Object, It.IsAny<string>())).ReturnsAsync(false);

            AuthController authController = new AuthController(mockNotificationService.Object, mockUserService.Object, mockJwtService.Object);
            var result = await authController.Login(mockAuthLoginDto.Object);

            var badResult = Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task LoginReturnsBadRequestWhenUserIsNotApprovedAsync()
        {
            mockUser.Setup(user => user.ApprovalStatus).Returns(false);
            mockUserService.Setup(userService => userService.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(mockUser.Object);
            mockUserService.Setup(userService => userService.CheckPassword(mockUser.Object, It.IsAny<string>())).ReturnsAsync(true);

            AuthController authController = new AuthController(mockNotificationService.Object, mockUserService.Object, mockJwtService.Object);
            var result = await authController.Login(mockAuthLoginDto.Object);

            var badResult = Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task LoginReturnsBadRequestWhenUserIsBlockedAsync()
        {
            mockUser.Setup(user => user.ApprovalStatus).Returns(true);
            mockUser.Setup(user => user.IsBlocked).Returns(true);
            mockUserService.Setup(userService => userService.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(mockUser.Object);
            mockUserService.Setup(userService => userService.CheckPassword(mockUser.Object, It.IsAny<string>())).ReturnsAsync(true);

            AuthController authController = new AuthController(mockNotificationService.Object, mockUserService.Object, mockJwtService.Object);
            var result = await authController.Login(mockAuthLoginDto.Object);

            var badResult = Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task RegisterMustCreateNewUserAsync()
        {
            AuthController authController = new AuthController(mockNotificationService.Object, mockUserService.Object, mockJwtService.Object);
            var result = await authController.Register(mockAuthRegisterDto.Object);
            
            mockUserService.Verify(userService => userService.CreateUser(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Once);
            mockUserService.Verify(userService => userService.AddUserRoleAsync(It.IsAny<string>(), RoleTypes.User), Times.Once);
            var okResult = Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task LogoutMustDeleteRefreshTokenAsync()
        {
            var mockClaimsPrincipal = new Mock<ClaimsPrincipal>();
            mockJwtService.Setup(jwtService => jwtService.DeleteRefreshTokenAsync(mockClaimsPrincipal.Object));

            AuthController authController = new AuthController(mockNotificationService.Object, mockUserService.Object, mockJwtService.Object);
            var result = await authController.Logout();

            Assert.IsType<OkResult>(result);
            mockJwtService.Verify(jwtService => jwtService.DeleteRefreshTokenAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
        }

        [Fact]
        public async Task RefreshReturnsChangedJwtTokenAsync()
        {
            var expectedTokens = new AuthTokensDto
            { AccessToken = "accessToken", RefreshToken = "refreshToken", ExpireOn = DateTime.Now };
            var mockClaimsPrincipal = new Mock<ClaimsPrincipal>();
            mockJwtService.Setup(jwtService => jwtService.GetPrincipalFromExpiredAccessToken(expectedTokens.AccessToken)).Returns(mockClaimsPrincipal.Object);
            mockJwtService.Setup(jwtService => jwtService.GenerateJwtAccessToken(mockClaimsPrincipal.Object.Claims)).Returns(It.IsAny<string>());
            mockJwtService.Setup(jwtService => jwtService.UpdateRefreshTokenAsync(expectedTokens.RefreshToken, mockClaimsPrincipal.Object)).ReturnsAsync(It.IsAny<string>());
            mockJwtService.SetupGet(jwtService => jwtService.ExpirationTime).Returns(DateTime.Now.AddMinutes(120));

            AuthController authController = new AuthController(mockNotificationService.Object, mockUserService.Object, mockJwtService.Object);
            var result = await authController.Refresh((AuthTokensDto)expectedTokens.Clone());

            mockJwtService.Verify();
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualTokens = Assert.IsAssignableFrom<AuthTokensDto>(okResult.Value);
            Assert.NotEqual(expectedTokens, actualTokens);
        }

        [Theory]
        [InlineData("token1", "token1")]
        [InlineData("token2", "token2")]
        [InlineData("token1", "token2")]
        [InlineData("token2", "token1")]
        [InlineData("somedata", "somedata")]
        public async Task RefreshReturnsNewValuesAsync(string accessToken, string refreshToken)
        {
            var expectedTokens = new AuthTokensDto
            { AccessToken = accessToken, RefreshToken = refreshToken, ExpireOn = DateTime.Now };
            var mockClaimsPrincipal = new Mock<ClaimsPrincipal>();
            var mockTokens = new Mock<AuthTokensDto>();
            mockJwtService.Setup(jwtService => jwtService.GetPrincipalFromExpiredAccessToken(It.IsAny<string>())).Returns(mockClaimsPrincipal.Object);
            mockJwtService.Setup(jwtService => jwtService.GenerateJwtAccessToken(mockClaimsPrincipal.Object.Claims)).Returns(expectedTokens.AccessToken);
            mockJwtService.Setup(jwtService => jwtService.UpdateRefreshTokenAsync(It.IsAny<string>(), mockClaimsPrincipal.Object)).ReturnsAsync(expectedTokens.RefreshToken);
            mockJwtService.SetupGet(jwtService => jwtService.ExpirationTime).Returns(DateTime.Now.AddMinutes(120));

            AuthController authController = new AuthController(mockNotificationService.Object, mockUserService.Object, mockJwtService.Object);
            var result = await authController.Refresh(mockTokens.Object);

            mockJwtService.Verify();
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualTokens = Assert.IsAssignableFrom<AuthTokensDto>(okResult.Value);
            Assert.Equal(expectedTokens, actualTokens);
        }

        [Fact]
        public async Task ForgetMustSendForgetPasswordMailForUserAsync()
        {
            var mockUserDto = new Mock<AuthMinimalDto>();
            mockUserService.Setup(userService => userService.GetUserByEmail(It.IsAny<string>())).ReturnsAsync(mockUser.Object);

            AuthController authController = new AuthController(mockNotificationService.Object, mockUserService.Object, mockJwtService.Object);
            var result = await authController.Forget(mockUserDto.Object);

            mockNotificationService.Verify(notificationService => notificationService.ForgetPasswordMail(mockUser.Object));
        }
    }
}
