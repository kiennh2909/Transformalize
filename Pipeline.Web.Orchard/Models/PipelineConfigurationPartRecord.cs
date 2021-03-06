#region license
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
using System.ComponentModel.DataAnnotations;
using Orchard.ContentManagement.Records;
using Orchard.Data.Conventions;

namespace Pipeline.Web.Orchard.Models {

   public class PipelineConfigurationPartRecord : ContentPartRecord {

      [StringLength(64)]
      public virtual string EditorMode { get; set; }

      [StringLengthMax]
      public virtual string Configuration { get; set; }

      [StringLength(64)]
      public virtual string StartAddress { get; set; }

      [StringLength(64)]
      public virtual string EndAddress { get; set; }

      public virtual bool Runnable { get; set; }

      public virtual bool Reportable { get; set; }

      public virtual bool ClientSideSorting { get; set; }
      public virtual int ClipTextAt { get; set; }

      public virtual bool NeedsInputFile { get; set; }

      [StringLength(128)]
      public virtual string Modes { get; set; }

      [StringLengthMax]
      public virtual string MapConfiguration { get; set; }

      public virtual int MapCircleRadius { get; set; }

      public virtual double MapCircleOpacity { get; set; }


      [StringLength(3)]
      public virtual string PlaceHolderStyle { get; set; }

      [StringLength(128)]
      public virtual string PageSizes { get; set; }
      [StringLength(128)]
      public virtual string MapSizes { get; set; }

      public virtual bool EnableInlineParameters { get; set; }

      public virtual bool CalendarEnabled { get; set; }
      public virtual bool CalendarPaging { get; set; }

      [StringLength(64)]
      public virtual string CalendarIdField { get; set; }
      [StringLength(64)]
      public virtual string CalendarTitleField { get; set; }
      [StringLength(64)]
      public virtual string CalendarUrlField { get; set; }
      [StringLength(64)]
      public virtual string CalendarClassField { get; set; }
      [StringLength(64)]
      public virtual string CalendarStartField { get; set; }
      [StringLength(64)]
      public virtual string CalendarEndField { get; set; }

      public virtual bool MapEnabled { get; set; }
      public virtual bool MapPaging { get; set; }
      public virtual bool MapBulkActions { get; set; }
      public virtual bool MapRefresh { get; set; }

      [StringLength(64)]
      public virtual string MapColorField { get; set; }
      [StringLength(64)]
      public virtual string MapPopUpField { get; set; }
      [StringLength(64)]
      public virtual string MapLatitudeField { get; set; }
      [StringLength(64)]
      public virtual string MapLongitudeField { get; set; }
      public virtual int MapZoom { get; set; }
      [StringLength(64)]
      public virtual string ReportRowClassField { get; set; }
      [StringLength(64)]
      public virtual string ReportRowStyleField { get; set; }

   }
}