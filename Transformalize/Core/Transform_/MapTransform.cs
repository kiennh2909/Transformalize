/*
Transformalize - Replicate, Transform, and Denormalize Your Data...
Copyright (C) 2013 Dale Newman

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Transformalize.Core.Parameters_;
using Transformalize.Extensions;
using Transformalize.Libs.Rhino.Etl.Core;

namespace Transformalize.Core.Transform_
{

    public class MapTransform : AbstractTransform
    {
        private readonly Map _equals;
        private readonly Map _startsWith;
        private readonly Map _endsWith;

        public MapTransform(IList<Map> maps, IParameters parameters) : base(parameters)
        {
            _equals = maps[0];
            _startsWith = maps[1];
            _endsWith = maps[2];
        }

        public override string Name
        {
            get { return "Map Transform"; }
        }

        public override bool RequiresParameters
        {
            get { return false; }
        }

        public override void Transform(ref StringBuilder sb)
        {
            foreach (var pair in _equals)
            {
                if (!sb.IsEqualTo(pair.Key)) continue;
                sb.Clear();
                sb.Append(pair.Value.Value);
                return;
            }

            foreach (var pair in _startsWith)
            {
                if (!sb.StartsWith(pair.Key)) continue;
                sb.Clear();
                sb.Append(pair.Value.Value);
                return;
            }

            foreach (var pair in _endsWith)
            {
                if (!sb.EndsWith(pair.Key)) continue;
                sb.Clear();
                sb.Append(pair.Value.Value);
                return;
            }

            if (!_equals.ContainsKey("*")) return;

            sb.Clear();
            sb.Append(_equals["*"].Value);
        }

        public override object Transform(object value)
        {
            var valueKey = value.ToString();

            if (_equals.ContainsKey(valueKey))
            {
                return _equals[valueKey].Value;
            }

            foreach (var pair in _startsWith.Where(pair => valueKey.StartsWith(pair.Key)))
            {
                return pair.Value.Value;
            }

            foreach (var pair in _endsWith.Where(pair => valueKey.EndsWith(pair.Key)))
            {
                return pair.Value.Value;
            }

            if (_equals.ContainsKey("*"))
            {
                return _equals["*"].Value;
            }

            return null;

        }

        public override void Transform(ref Row row, string resultKey)
        {
            var valueKey = row[FirstParameter.Key].ToString();
            
            if (_equals.ContainsKey(valueKey))
            {
                row[resultKey] = _equals[valueKey].Value ?? row[_equals[valueKey].Parameter];
                return;
            }

            foreach (var pair in _startsWith.Where(pair => valueKey.StartsWith(pair.Key)))
            {
                row[resultKey] = pair.Value.Value ?? row[pair.Value.Parameter];
                return;
            }

            foreach (var pair in _endsWith.Where(pair => valueKey.EndsWith(pair.Key)))
            {
                row[resultKey] = pair.Value.Value ?? row[pair.Value.Parameter];
                return;
            }

            if (_equals.ContainsKey("*"))
            {
                row[resultKey] = _equals["*"].Value ?? row[_equals["*"].Parameter];
            }

        }

    }
}