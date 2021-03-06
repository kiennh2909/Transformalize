﻿#region license
// Transformalize
// Copyright 2013 Dale Newman
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//  
//      http://www.apache.org/licenses/LICENSE-2.0
//  
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using Orchard;
using Orchard.ContentManagement;
using Orchard.Logging;
using Orchard.Themes;
using Orchard.UI.Notify;
using Pipeline.Web.Orchard.Models;
using Pipeline.Web.Orchard.Services;
using Pipeline.Web.Orchard.Services.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using Transformalize.Contracts;
using Permissions = Orchard.Core.Contents.Permissions;
using Process = Transformalize.Configuration.Process;

namespace Pipeline.Web.Orchard.Controllers {
   [ValidateInput(false), Themed]
   public class ReportController : BaseController {

      private readonly IOrchardServices _orchardServices;
      private readonly IProcessService _processService;
      private readonly ISortService _sortService;
      private readonly ISecureFileService _secureFileService;

      public ReportController(
          IOrchardServices services,
          IProcessService processService,
          ISortService sortService,
          ISecureFileService secureFileService
      ) {
         _orchardServices = services;
         _processService = processService;
         _secureFileService = secureFileService;
         _sortService = sortService;
      }

      [Themed(true)]
      public ActionResult Index(int id) {

         var timer = new Stopwatch();
         timer.Start();
         var user = _orchardServices.WorkContext.CurrentUser == null ? "Anonymous" : _orchardServices.WorkContext.CurrentUser.UserName ?? "Anonymous";

         Logger.Information("User {0} has requested report id {1}", user, id);

         var process = new Process { Name = "Report" };

         var part = _orchardServices.ContentManager.Get(id).As<PipelineConfigurationPart>();
         if (part == null) {
            process.Name = "Not Found";
         } else {

            if (_orchardServices.Authorizer.Authorize(Permissions.ViewContent, part)) {

               var title = part.Title();
               Logger.Information("User {0} is authorized to view report {1}", user, title);

               process = _processService.Resolve(part);

               Logger.Information("User {0} has resolved process for {1}", user, title);               

               var parameters = Common.GetParameters(Request, _orchardServices, _secureFileService);
               if (part.NeedsInputFile && Convert.ToInt32(parameters[Common.InputFileIdName]) == 0) {
                  _orchardServices.Notifier.Add(NotifyType.Error, T("This transformalize expects a file."));
                  process.Name = "File Not Found";
               }

               GetStickyParameters(part.Id, parameters);
               Logger.Information("User {0} has gathered parameters for {1}", user, title);

               process.Load(part.Configuration, parameters);
               Logger.Information("User {0} has has loaded process for {1}", user, title);

               process.Mode = "report";
               process.ReadOnly = true;  // force reporting to omit system fields

               SetStickyParameters(part.Id, process.Parameters);
               Logger.Information("User {0} has set sticky parameters for {1}", user, title);

               // secure actions
               var actions = process.Actions.Where(a => !a.Before && !a.After && !a.Description.StartsWith("Batch", StringComparison.OrdinalIgnoreCase));
               foreach (var action in actions) {
                  var p = _orchardServices.ContentManager.Get(action.Id);
                  if (!_orchardServices.Authorizer.Authorize(Permissions.ViewContent, p)) {
                     action.Description = "BatchUnauthorized";
                  }
               }
               Logger.Information("User {0} has has secured actions for {1}", user, title);

               var sizes = new List<int>();
               sizes.AddRange(part.Sizes(part.PageSizes));
               var stickySize = GetStickyParameter(part.Id, "size", () => sizes.Min());

               Common.SetPageSize(process, parameters, sizes.Min(), stickySize, sizes.Max());

               Logger.Information("User {0} has resolved page sizes for {1}", user, title);

               if (Request["sort"] != null) {
                  _sortService.AddSortToEntity(process.Entities.First(), Request["sort"]);
                  Logger.Information("User {0} has resolved sorting for {1}", user, title);
               }

               if (process.Errors().Any()) {
                  foreach (var error in process.Errors()) {
                     _orchardServices.Notifier.Add(NotifyType.Error, T(error));
                  }
               } else {
                  if (process.Entities.Any(e => !e.Fields.Any(f => f.Input))) {
                     _orchardServices.WorkContext.Resolve<ISchemaHelper>().Help(process);
                  }
                  
                  if (part.ReportRowClassField != string.Empty || part.ReportRowStyleField != string.Empty) {
                     var fieldAliases = new HashSet<string>(process.GetAllFields().Select(f => f.Alias));
                     if (part.ReportRowClassField != string.Empty && !fieldAliases.Contains(part.ReportRowClassField)){
                        _orchardServices.Notifier.Error(T("Can not find report row class field {0}", part.ReportRowClassField));
                        return View(new ReportViewModel(process, part));
                     }
                     if (part.ReportRowStyleField != string.Empty && !fieldAliases.Contains(part.ReportRowStyleField)) {
                        _orchardServices.Notifier.Error(T("Can not find report row style field {0}", part.ReportRowStyleField));
                        return View(new ReportViewModel(process, part));
                     }
                  }
                  Logger.Information("User {0} has resolved row class and style for {1}", user, title);

                  if (!process.Errors().Any()) {

                     if (IsMissingRequiredParameters(process.Parameters, _orchardServices.Notifier)) {
                        return View(new ReportViewModel(process, part));
                     }

                     var runner = _orchardServices.WorkContext.Resolve<IRunTimeExecute>();

                     Logger.Information("User {0} has resolved runner for {1}", user, title);

                     try {

                        runner.Execute(process);
                        Logger.Information("User {0} has executed runner for {1}", user, title);

                        process.Request = "Run";
                        process.Time = timer.ElapsedMilliseconds;

                        if (process.Errors().Any()) {
                           foreach (var error in process.Errors()) {
                              _orchardServices.Notifier.Add(NotifyType.Error, T(error));
                           }
                           process.Status = 500;
                           process.Message = "There are errors in the pipeline.  See log.";
                        } else {
                           process.Status = 200;
                           process.Message = "Ok";
                        }

                     } catch (Exception ex) {
                        Logger.Error(ex, ex.Message);
                        _orchardServices.Notifier.Error(T(ex.Message));
                     }

                  }
               }

            } else {
               _orchardServices.Notifier.Warning(user == "Anonymous" ? T("Sorry. Anonymous users do not have permission to view this report. You may need to login.") : T("Sorry {0}. You do not have permission to view this report.", user));
            }
         }

         if(Request.QueryString["Style"] == "ajax") {
            return View("Ajax", new ReportViewModel(process, part));
         } else {
            return View(new ReportViewModel(process, part));
         }

      }
   }
}