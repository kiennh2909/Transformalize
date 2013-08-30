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

using System.Text;

namespace Transformalize.Core.Transform_ {
    public class RemoveTransform : AbstractTransform {
        private readonly int _startIndex;
        private readonly int _length;

        public RemoveTransform(int startIndex, int length) {
            _startIndex = startIndex;
            _length = length;
        }

        public override string Name {
            get { return "Remove Transform"; }
        }

        public override bool RequiresParameters
        {
            get { return false; }
        }

        public override void Transform(ref StringBuilder sb) {
            if (_startIndex > sb.Length) return;
            sb.Remove(_startIndex, _length);
        }

        public override object Transform(object value) {
            var str = value.ToString();
            if (_startIndex > str.Length) return value;
            return str.Remove(_startIndex, _length);
        }
    }
}