﻿using Microsoft.AspNetCore.Mvc;
using BookingApp.Helpers;
using System.Linq;

namespace BookingApp.Controllers
{
    /// <summary>
    /// Base controller class having cross-entity functional.
    /// </summary>
    public abstract class EntityControllerBase : ControllerBase
    {
        /// <summary>
        /// Current user identifier
        /// </summary>
        protected string UserId => User.Claims.Single(c => c.Type == "uid").Value;

        /// <summary>
        /// Shorthand for checking if current user has admin access level
        /// </summary>
        protected bool IsAdmin => User.IsInRole(RoleTypes.Admin);

        /// <summary>
        /// Shorthand for checking if current user has user access level
        /// </summary>
        protected bool IsUser => User.IsInRole(RoleTypes.User);

        /// <summary>
        /// Shorthand for checking if current user doesn't have any specific rights.
        /// Current user is anonymous if he hasn't UserID claim.
        /// </summary>
        protected bool IsAnonymous => !User.HasClaim(c => c.Type == "uid");

        /// <summary>
        /// Gets full base api url of a current controller, if annotated correctly.
        /// </summary>
        protected string BaseApiUrl
        {
            get
            {
                var request = ControllerContext.HttpContext.Request;
                return request.Scheme + "://" + request.Host + request.Path;
            }
        }
    }
}