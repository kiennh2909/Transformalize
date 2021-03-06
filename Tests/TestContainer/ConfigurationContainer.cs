﻿#region license
// Transformalize
// Configurable Extract, Transform, and Load
// Copyright 2013-2017 Dale Newman
//  
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//       http://www.apache.org/licenses/LICENSE-2.0
//   
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using Autofac;
using Autofac.Core;
using Cfg.Net.Contracts;
using Cfg.Net.Environment;
using Cfg.Net.Reader;
using Cfg.Net.Shorthand;
using System;
using System.Collections.Generic;
using System.Linq;
using Tests.TestContainer.Modules;
using Tests.TestContainers;
using Transformalize;
using Transformalize.Configuration;
using Transformalize.Context;
using Transformalize.Contracts;
using Process = Transformalize.Configuration.Process;

namespace Tests.TestContainer {

   /// <summary>
   /// This container deals with the arrangement and how it becomes a process.
   /// Transforms and Validators are registered here as well because their 
   /// short-hand is expanded in the arrangement before it becomes a process.
   /// </summary>
   public class ConfigurationContainer {

      private readonly List<IModule> _modules = new List<IModule>();
      private readonly List<TransformHolder> _transforms = new List<TransformHolder>();
      private readonly List<ValidatorHolder> _validators = new List<ValidatorHolder>();

      public ConfigurationContainer() { }

      /// <summary>
      /// for registering modules that require short-hand (i.e. transforms and validators)
      /// that are not in the plugins folder.
      /// </summary>
      /// <param name="args"></param>
      public ConfigurationContainer(params IModule[] args) {
         _modules.AddRange(args);
      }

      public ConfigurationContainer(params TransformHolder[] transforms) {
         _transforms.AddRange(transforms);
      }

      public ConfigurationContainer(params ValidatorHolder[] validators) {
         _validators.AddRange(validators);
      }

      private readonly HashSet<string> _methods = new HashSet<string>();
      private readonly ShorthandRoot _shortHand = new ShorthandRoot();
      public ILifetimeScope CreateScope(string cfg, IPipelineLogger logger, IDictionary<string, string> parameters = null, string placeHolderStyle = "@[]") {

         if (placeHolderStyle == null || placeHolderStyle.Length != 3) {
            throw new ArgumentException("The placeHolderStyle parameter must be three characters (e.g. @[], or @())");
         }

         if (parameters == null) {
            parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
         }

         var builder = new ContainerBuilder();

         builder.Register((ctx) => logger).As<IPipelineLogger>();

         builder.Register<IReader>(c => new DefaultReader(new FileReader(), new WebReader())).As<IReader>();

         // register short-hand for t attribute
         var transformModule = new TransformModule(new Process { Name = "TransformShorthand" }, _methods, _shortHand, logger);
         foreach (var t in _transforms) {
            transformModule.AddTransform(t);
         }
         builder.RegisterModule(transformModule);

         // register short-hand for v attribute
         var validateModule = new ValidateModule(new Process { Name = "ValidateShorthand" }, _methods, _shortHand, logger);
         foreach (var v in _validators) {
            validateModule.AddValidator(v);
         }
         builder.RegisterModule(validateModule);

         // register the validator short hand
         builder.Register((c, p) => _shortHand).Named<ShorthandRoot>(ValidateModule.Name).InstancePerLifetimeScope();
         builder.Register((c, p) => new ShorthandCustomizer(c.ResolveNamed<ShorthandRoot>(ValidateModule.Name), new[] { "fields", "calculated-fields", "calculatedfields" }, "v", "validators", "method")).Named<IDependency>(ValidateModule.Name).InstancePerLifetimeScope();

         // register the transform short hand
         builder.Register((c, p) => _shortHand).Named<ShorthandRoot>(TransformModule.FieldsName).InstancePerLifetimeScope();
         builder.Register((c, p) => _shortHand).Named<ShorthandRoot>(TransformModule.ParametersName).InstancePerLifetimeScope();
         builder.Register((c, p) => new ShorthandCustomizer(c.ResolveNamed<ShorthandRoot>(TransformModule.FieldsName), new[] { "fields", "calculated-fields", "calculatedfields" }, "t", "transforms", "method")).Named<IDependency>(TransformModule.FieldsName).InstancePerLifetimeScope();
         builder.Register((c, p) => new ShorthandCustomizer(c.ResolveNamed<ShorthandRoot>(TransformModule.ParametersName), new[] { "parameters" }, "t", "transforms", "method")).Named<IDependency>(TransformModule.ParametersName).InstancePerLifetimeScope();

#if PLUGINS
         // the shorthand registrations are stored in the builder's properties for plugins to add to
         builder.Properties["ShortHand"] = _shortHand;
         builder.Properties["Methods"] = _methods;
#endif

         foreach (var module in _modules) {
            builder.RegisterModule(module);
         }

         builder.Register((c, p) => _methods).Named<HashSet<string>>("Methods").InstancePerLifetimeScope();
         builder.Register((c, p) => _shortHand).As<ShorthandRoot>().InstancePerLifetimeScope();

         builder.Register(ctx => {

            var transformed = TransformParameters(ctx, cfg);

            var process = new Process(transformed ?? cfg, parameters, new List<IDependency> {
               ctx.Resolve<IReader>(),
               new FormParameterModifier(new DateMathModifier()),
               new ParameterModifier(new PlaceHolderReplacer(placeHolderStyle[0], placeHolderStyle[1], placeHolderStyle[2]),"parameters", "name", "value"),
               ctx.ResolveNamed<IDependency>(TransformModule.FieldsName),
               ctx.ResolveNamed<IDependency>(TransformModule.ParametersName),
               ctx.ResolveNamed<IDependency>(ValidateModule.Name)
            }.ToArray());

            if (process.Errors().Any()) {
               var c = new PipelineContext(logger, new Process() { Name = "Errors" });
               c.Error("The configuration has errors.");
               foreach (var error in process.Errors()) {
                  c.Error(error);
               }
            }

            return process;
         }).As<Process>().InstancePerDependency();
         return builder.Build().BeginLifetimeScope();
      }

      private static string TransformParameters(IComponentContext ctx, string cfg) {

         var preProcess = new Transformalize.ConfigurationFacade.Process(
            cfg,
            new Dictionary<string, string>(),
            new List<IDependency> {
               ctx.Resolve<IReader>(),
               new DateMathModifier(),
               new ParameterModifier(new NullPlaceHolderReplacer()),
               ctx.ResolveNamed<IDependency>(TransformModule.ParametersName)
         }.ToArray());

         if (!preProcess.Parameters.Any(pr => pr.Transforms.Any()))
            return null;

         var fields = preProcess.Parameters.Select(pr => new Field {
            Name = pr.Name,
            Alias = pr.Name,
            Default = pr.Value,
            Type = pr.Type,
            Transforms = pr.Transforms.Select(o => o.ToOperation()).ToList()
         }).ToList();
         var len = fields.Count;
         var entity = new Entity { Name = "Parameters", Alias = "Parameters", Fields = fields };
         var mini = new Process {
            Name = "ParameterTransform",
            ReadOnly = true,
            Entities = new List<Entity> { entity },
            Maps = preProcess.Maps.Select(m => m.ToMap()).ToList(), // for map transforms
            Scripts = preProcess.Scripts.Select(s => s.ToScript()).ToList() // for transforms that use scripts (e.g. js)
         };

         mini.Check(); // very important to check after creating, as it runs validation and even modifies!

         if (!mini.Errors().Any()) {

            // modification in Check() do not make it out to local variables so overwrite them
            fields = mini.Entities.First().Fields;
            entity = mini.Entities.First();

            var c = new PipelineContext(ctx.Resolve<IPipelineLogger>(), mini, entity);
            var transforms = new List<ITransform> {
               new Transformalize.Transforms.System.DefaultTransform(c, fields)
            };
            transforms.AddRange(TransformFactory.GetTransforms(ctx, c, fields));

            // make an input out of the parameters
            var input = new List<IRow>();
            var row = new MasterRow(len);
            for (var i = 0; i < len; i++) {
               row[fields[i]] = preProcess.Parameters[i].Value;
            }

            input.Add(row);

            var output = transforms.Aggregate(input.AsEnumerable(), (rows, t) => t.Operate(rows)).ToList().First();

            for (var i = 0; i < len; i++) {
               var parameter = preProcess.Parameters[i];
               parameter.Value = output[fields[i]].ToString();
               parameter.T = string.Empty;
               parameter.Transforms.Clear();
            }

            return preProcess.Serialize();
         }

         var context = new PipelineContext(ctx.Resolve<IPipelineLogger>(), mini, entity);
         foreach (var error in mini.Errors()) {
            context.Error(error);
         }

         return null;
      }

      public void AddValidator(Func<IContext, IValidate> getValidator, IEnumerable<OperationSignature> signatures) {
         _validators.Add(new ValidatorHolder(getValidator, signatures));
      }
      public void AddTransform(Func<IContext, ITransform> getTransform, IEnumerable<OperationSignature> signatures) {
         _transforms.Add(new TransformHolder(getTransform, signatures));
      }

   }

}
