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
using System;

namespace Transformalize.Configuration {

    public class Pagination {
        public int Pages { get; }
        public bool HasPrevious { get; private set; }
        public bool HasNext { get; private set; }
        public int Previous { get; private set; }
        public int Next { get; private set; }
        public int First { get; private set; }
        public int Last { get; private set; }

        public Pagination(int hits, int page, int pageSize) {
            var pages = pageSize == 0 ? 0 : (int)Math.Ceiling((decimal)hits / pageSize);
            Pages = pages;
            HasPrevious = page > 1;
            Previous = page == 1 ? 1 : page - 1;
            HasNext = page < pages;
            Next = page == pages ? page : page + 1;
            Last = Pages;
            First = 1;
        }
    }
}