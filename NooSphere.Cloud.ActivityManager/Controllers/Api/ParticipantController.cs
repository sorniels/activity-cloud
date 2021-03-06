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
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NooSphere.Cloud.ActivityManager.Authentication;
using NooSphere.Cloud.ActivityManager.Events;
using NooSphere.Core.ActivityModel;

#endregion

namespace NooSphere.Cloud.ActivityManager.Controllers.Api
{
    public class ParticipantController : BaseController
    {
        private readonly ActivityController ActivityController = new ActivityController();
        private readonly UserController UserController = new UserController();

        #region Exposed API Methods

        /// <summary>
        ///   Add participant to the specified activity.
        /// </summary>
        /// <param name="activityId"> Guid representation of the activity Id. </param>
        /// <param name="participantId"> Guid representation of the user Id. </param>
        /// <returns> Returns true if participant was added, false if not. </returns>
        [RequireUser]
        public bool Post(Guid activityId, Guid participantId)
        {
            try
            {
                JObject activity = ActivityController.GetExtendedActivity(activityId);
                JObject participant = UserController.GetExtendedUser(participantId);

                List<JObject> participants = activity["Participants"].Children<JObject>().ToList();
                participants.Add(participant);
                activity["Participants"] = JToken.FromObject(participants);

                ActivityController.UpdateActivity(NotificationType.None, activity);
                Notifier.NotifyGroup(activityId, NotificationType.ParticipantAdded,
                                     new {ActivityId = activityId, Participant = participant});
                Notifier.NotifyGroup(participantId, NotificationType.ActivityAdded, activity);
                return true;
            } catch(Exception)
            {
                return false;
            }
        }

        /// <summary>
        ///   Remove a participant from the specified activity.
        /// </summary>
        /// <param name="activityId"> Guid representation of the activity Id. </param>
        /// <param name="participantId"> Guid representation of the user Id. </param>
        /// <returns> Returns true if participant was removed, false if not. </returns>
        [RequireUser]
        public bool Delete(Guid activityId, Guid participantId)
        {
            if (activityId != null && participantId != null)
            {
                Activity activity = ActivityController.GetActivity(activityId);
                JObject participant = UserController.GetExtendedUser(participantId);
                List<User> participants = activity.Participants.Where(u => u.Id != participantId).ToList();

                var result = new List<JObject>();
                foreach (User p in participants)
                    result.Add(UserController.GetExtendedUser(p.Id));

                JObject completeActivity = ActivityController.GetExtendedActivity(activityId);
                completeActivity["Participants"] = JToken.FromObject(result);

                ActivityController.UpdateActivity(NotificationType.None, completeActivity);
                Notifier.NotifyGroup(activityId, NotificationType.ParticipantRemoved,
                                     new {ActivityId = activityId, ParticipantId = participantId});
                Notifier.NotifyGroup(participantId, NotificationType.ActivityDeleted, new {activity.Id});

                return true;
            }
            return false;
        }

        #endregion
    }
}