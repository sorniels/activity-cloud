﻿#region License

// Copyright (c) 2012 Steven Houben(shou@itu.dk) and Søren Nielsen(snielsen@itu.dk)
// 
// Pervasive Interaction Technology Laboratory (pIT lab)
// IT University of Copenhagen
// 
// This library is free software; you can redistribute it and/or 
// modify it under the terms of the GNU GENERAL PUBLIC LICENSE V3 or later, 
// as published by the Free Software Foundation. Check 
// http://www.gnu.org/licenses/gpl.html for details.

#endregion

#region

using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using NooSphere.Cloud.ActivityManager.Authentication;
using NooSphere.Cloud.ActivityManager.Events;
using NooSphere.Cloud.Data.Storage;
using NooSphere.Core.ActivityModel;

#endregion

namespace NooSphere.Cloud.ActivityManager.Controllers.Api
{
    public class FileController : BaseController
    {
        private readonly FileStorage _fileStorage = new FileStorage(
            ConfigurationManager.AppSettings["AmazonAccessKeyId"],
            ConfigurationManager.AppSettings["AmazonSecretAccessKey"]);

        #region Exposed API Methods

        /// <summary>
        ///   Download the resource.
        /// </summary>
        /// <param name="activityId"> Guid representation of the activity Id. </param>
        /// <param name="resourceId"> Guid representation of the resource Id. </param>
        /// <returns> byte[] of the given resource </returns>
        [RequireUser]
        public HttpResponseMessage Get(Guid activityId, Guid resourceId)
        {
            var response = new HttpResponseMessage();
            var stream = _fileStorage.Download(GenerateId(activityId, resourceId));
            if (stream != null)
            {
                response.StatusCode = HttpStatusCode.OK;
                response.Content = new StreamContent(stream);
            }
            else
                response.StatusCode = HttpStatusCode.NotFound;
            return response;
        }

        /// <summary>
        ///   Upload the resource
        /// </summary>
        /// <param name="activityId"> Guid representation of the activity Id. </param>
        /// <param name="resourceId"> Guid representation of the resource Id. </param>
        [RequireUser]
        public Task<HttpResponseMessage> Post(Guid activityId, Guid resourceId)
        {

            var r = new Resource
                        {
                            Id = resourceId,
                            ActivityId = activityId
                        };

            var task = Request.Content.ReadAsStreamAsync();
            var result = task.ContinueWith(o =>
            {
                if (_fileStorage.Upload(GenerateId(r), task.Result))
                    Notifier.NotifyGroup(activityId, NotificationType.FileDownload, r);
                    return new HttpResponseMessage { StatusCode = HttpStatusCode.OK };
            });

            return result;
        }

        #endregion

        #region Public Methods

        [NonAction]
        public void Sync(Activity activity, SyncType type)
        {
            foreach (Resource resource in activity.Resources)
            {
                if (type == SyncType.Added)
                    Notifier.NotifyGroup(activity.Id, NotificationType.FileUpload, resource);
                else if (type == SyncType.Removed)
                    Notifier.NotifyGroup(activity.Id, NotificationType.FileDelete, resource);
                else if (type == SyncType.Updated)
                {
                    if (DateTime.Parse(resource.LastWriteTime) > _fileStorage.LastWriteTime(GenerateId(resource)))
                        Notifier.NotifyGroup(ConnectionId, NotificationType.FileUpload, resource);
                }
            }
        }

        #endregion

        #region Private Methods

        private string GenerateId(Guid activityId, Guid resourceId)
        {
            return "Activities/" + activityId + "/Resources/" + resourceId;
        }

        private string GenerateId(Resource r)
        {
            return "Activities/" + r.ActivityId + "/Resources/" + r.Id;
        }

        #endregion
    }

    public enum SyncType
    {
        Added,
        Removed,
        Updated
    }
}