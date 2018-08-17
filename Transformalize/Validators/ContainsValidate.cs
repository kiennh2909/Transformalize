#region license
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

using System.Collections.Generic;
using System.Linq;
using Transformalize.Configuration;
using Transformalize.Contracts;
using Transformalize.Transforms;

namespace Transformalize.Validators {
    public class ContainsValidator : StringValidate {
        private readonly Field[] _input;
        private readonly Field _valueField;
        private readonly bool _valueIsField;
        private readonly BetterFormat _betterFormat;

        public ContainsValidator(IContext context = null) : base(context) {

            if (IsMissingContext()) {
                return;
            }

            if (!Run)
                return;

            if (IsMissing(Context.Operation.Value)) {
                return;
            }
            _input = MultipleInput();
            _valueIsField = Context.Entity.TryGetField(Context.Operation.Value, out _valueField);

            var help = Context.Field.Help;
            if (help == string.Empty) {
                if (_valueIsField) {
                    help = $"{Context.Field.Label} must contain {{{_valueField.Alias}}}.";
                } else {
                    help = $"{Context.Field.Label} must contain {Context.Operation.Value}.";
                }
            }
            _betterFormat = new BetterFormat(context, help, Context.Entity.GetAllFields);

        }

        public override IRow Operate(IRow row) {
            var valueItMustContain = _valueIsField ? GetString(row, _valueField) : Context.Operation.Value;
            var valid = _input.Any(f => GetString(row, f).Contains(valueItMustContain));
            if (IsInvalid(row, valid)) {
                AppendMessage(row, _betterFormat.Format(row));
            }

            return row;
        }

        public override IEnumerable<OperationSignature> GetSignatures() {
            yield return new OperationSignature("contains") { Parameters = new List<OperationParameter>(1) { new OperationParameter("value") } };
        }
    }
}