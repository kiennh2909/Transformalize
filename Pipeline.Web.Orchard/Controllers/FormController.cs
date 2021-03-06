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

using System;
using System.Linq;
using System.Web.Mvc;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Themes;
using Orchard.UI.Notify;
using Transformalize.Contracts;
using Pipeline.Web.Orchard.Models;
using Pipeline.Web.Orchard.Services;
using Pipeline.Web.Orchard.Services.Contracts;
using Process = Transformalize.Configuration.Process;
using Permissions = Orchard.Core.Contents.Permissions;
using System.Collections.Generic;
using ZXing;
using System.Drawing;
using ZXing.Multi;
using ZXing.Common;
using ZXing.QrCode;

namespace Pipeline.Web.Orchard.Controllers {

   [ValidateInput(false), Themed]
   public class FormController : Controller {

      private readonly IOrchardServices _orchardServices;
      private readonly IProcessService _processService;
      private readonly ISecureFileService _secureFileService;
      private readonly IFileService _fileService;
      public Localizer T { get; set; }
      public ILogger Logger { get; set; }

      public FormController(
          IOrchardServices services,
          IProcessService processService,
          ISecureFileService secureFileService,
          IFileService fileService
      ) {
         _orchardServices = services;
         _processService = processService;
         _secureFileService = secureFileService;
         _fileService = fileService;
         T = NullLocalizer.Instance;
         Logger = NullLogger.Instance;
      }

      [Themed(true)]
      public ActionResult Index(int id) {

         var part = _orchardServices.ContentManager.Get(id).As<PipelineConfigurationPart>();
         if (part == null) {
            return new HttpNotFoundResult("Form not found.");
         }
         var user = _orchardServices.WorkContext.CurrentUser == null ? "Anonymous" : _orchardServices.WorkContext.CurrentUser.UserName ?? "Anonymous";

         if (_orchardServices.Authorizer.Authorize(Permissions.ViewContent, part)) {

            var process = _processService.Resolve(part);
            var parameters = Common.GetParameters(Request, _orchardServices, _secureFileService);

            SwapFileParamaters(parameters);

            process.Load(part.Configuration, parameters);

            if (process.Errors().Any() || process.Warnings().Any()) {
               return View(new FormViewModel(Request, _orchardServices, part, process));
            }

            if (process.Parameters.Any(p => p.Name.Equals("id", StringComparison.OrdinalIgnoreCase))) {
               _orchardServices.Notifier.Error(T("You can't use a paramater named 'id' in a form."));
            }

            var runner = _orchardServices.WorkContext.Resolve<IRunTimeExecute>();
            var entity = process.Entities.First();
            runner.Execute(process);

            if (Request.HttpMethod.Equals("POST")) {

               if (entity.Rows.Count == 1 && (bool)entity.Rows[0][entity.ValidField]) {
                  // reset, modify for actual insert, and execute again
                  process = _processService.Resolve(part);
                  process.Load(part.Configuration, parameters);
                  var insert = entity.GetPrimaryKey().All(k => parameters.ContainsKey(k.Alias) && k.Default == parameters[k.Alias]);
                  process.Actions.Add(new Transformalize.Configuration.Action {
                     After = true,
                     Before = false,
                     Type = "run",
                     Connection = entity.Connection,
                     Command = insert ? entity.InsertCommand : entity.UpdateCommand,
                     Key = Guid.NewGuid().ToString(),
                     ErrorMode = "exception"
                  });

                  try {
                     runner.Execute(process);
                     _orchardServices.Notifier.Information(insert ? T("{0} inserted", process.Name) : T("{0} updated", process.Name));
                     return Redirect(parameters["Orchard.ReturnUrl"]);
                  } catch (Exception ex) {
                     if (ex.Message.Contains("duplicate")) {
                        _orchardServices.Notifier.Error(T("The {0} save failed: {1}", process.Name, "The database has rejected this update due to a unique constraint violation."));
                     } else {
                        _orchardServices.Notifier.Error(T("The {0} save failed: {1}", process.Name, ex.Message));
                     }
                     Logger.Error(ex, ex.Message);
                  }
               } else {
                  _orchardServices.Notifier.Error(T("The form did not pass validation.  Please correct it and re-submit."));
               }

            }
            var model = new FormViewModel(Request, _orchardServices, part, process);
            if (model.Geo) {
               model.Latitude = Request.Form["Orchard.Latitude"] ?? string.Empty;
               model.Longitude = Request.Form["Orchard.Longitude"] ?? string.Empty;
               model.Accuracy = Request.Form["Orchard.Accuracy"] ?? string.Empty;
            }

            return View(model);
         }

         _orchardServices.Notifier.Warning(user == "Anonymous" ? T("Anonymous users do not have permission to view this form. You may need to login.") : T("Sorry {0}. You do not have permission to view this form.", user));
         return new HttpUnauthorizedResult();
      }

      /// <summary>
      /// file fields actually store the content item id stored in the _Old input
      /// so we have to swap them into the file field for validation and storage
      /// </summary>
      /// <param name="parameters"></param>
      private void SwapFileParamaters(IDictionary<string, string> parameters) {
         var swaps = parameters.Where(i => i.Key.EndsWith("_Old")).ToArray();
         foreach (var swap in swaps) {
            var key = swap.Key.Substring(0, swap.Key.Length - 4);
            parameters[key] = swap.Value;
         }
      }

      [Themed(false)]
      public ActionResult Content(int id) {

         var process = new Process { Name = "Form", Id = id };
         var part = _orchardServices.ContentManager.Get(id).As<PipelineConfigurationPart>();
         if (part == null) {
            process.Status = 404;
            process.Message = "Not Found";
         } else {
            if (_orchardServices.Authorizer.Authorize(Permissions.ViewContent, part)) {

               var runner = _orchardServices.WorkContext.Resolve<IRunTimeExecute>();
               process = _processService.Resolve(part);
               var parameters = Common.GetParameters(Request, _orchardServices, _secureFileService);
               SwapFileParamaters(parameters);
               process.Load(part.Configuration, parameters);
               runner.Execute(process);
            } else {
               process.Message = "Unauthorized";
               process.Status = 401;
            }
         }
         return View(new FormViewModel(Request, _orchardServices, part, process));
      }

      [Themed(false), HttpPost]
      public ContentResult Upload() {

         if (!User.Identity.IsAuthenticated) {
            return GetResult(0, "Unauthorized");
         }

         if (Request.Files != null && Request.Files.Count > 0) {
            var file = Request.Files[0];
            if (file != null && file.ContentLength > 0) {
               var filePart = _fileService.Upload(file, "Authenticated", "Forms", 1);
               return GetResult(filePart.Id, file.FileName);
            }
         }

         return GetResult(0, "Error");
      }

      [Themed(false), HttpPost]
      public ContentResult Scan() {

         if (!User.Identity.IsAuthenticated) {
            return GetResult(0, "Unauthorized");
         }

         if (Request.Files != null && Request.Files.Count > 0) {
            var file = Request.Files[0];
            if (file != null && file.ContentLength > 0) {

               var bitMap = (Bitmap)Image.FromStream(file.InputStream);
               var source = new BitmapLuminanceSource(bitMap);
               var binarizer = (Binarizer) new HybridBinarizer(source);
               var binaryBitMap = new BinaryBitmap(binarizer);

               var reader = new MultiFormatReader();
               // if you need to find multiple barcodes in same images, var reader = new GenericMultipleBarcodeReader(multiFormatReader);
               
               var hints = new Dictionary<DecodeHintType, object>() {
                  {DecodeHintType.PURE_BARCODE, false},
                  {DecodeHintType.TRY_HARDER, true }
               };
               var result = reader.decode(binaryBitMap, hints);
               if (result == null) {
                  binarizer = new GlobalHistogramBinarizer(source);
                  binaryBitMap = new BinaryBitmap(binarizer);
                  result = reader.decode(binaryBitMap, hints);
                  if (result == null) {
                     return GetResult(0, string.Empty);
                  } else {
                     return GetResult(1, result.Text);
                  }
               } else {
                  return GetResult(1, result.Text);
               }
            }
         }

         return GetResult(0, "Error");
      }

      private static ContentResult GetResult(int id, string message) {
         var data = string.Format("{{ \"id\":{0}, \"message\":\"{1}\" }}", id, message);
         return new ContentResult {
            Content = data,
            ContentType = "text/json"
         };
      }

   }
}