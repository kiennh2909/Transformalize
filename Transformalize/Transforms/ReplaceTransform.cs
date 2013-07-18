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
using System.Text;
using Transformalize.Model;

namespace Transformalize.Transforms {
    public class ReplaceTransform : Transformer {

        private readonly string _oldValue;
        private readonly string _newValue;

        public ReplaceTransform(string oldValue, string newValue) {
            _oldValue = oldValue;
            _newValue = newValue;
        }

        protected override string Name {
            get { return "Replace Transform"; }
        }

        public override void Transform(ref StringBuilder sb) {
            sb.Replace(_oldValue, _newValue);
        }

        public override void Transform(ref object value) {
            value = value.ToString().Replace(_oldValue, _newValue);
        }
    }
}