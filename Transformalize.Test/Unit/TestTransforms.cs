﻿using System;
using System.Collections.Generic;
using NUnit.Framework;
using Moq;
using Transformalize.Model;
using Transformalize.Operations;
using Transformalize.Rhino.Etl.Core;
using Transformalize.Rhino.Etl.Core.Operations;
using Transformalize.Transforms;

namespace Transformalize.Test.Unit {
    [TestFixture]
    public class TestTransforms : EtlProcessHelper {

        private readonly Mock<IOperation> _testInput = new Mock<IOperation>();

        [SetUp]
        public void SetUp() {
            _testInput.Setup(foo => foo.Execute(It.IsAny<IEnumerable<Row>>())).Returns(new List<Row> {
                new Row { {"Field1", "A b C d E f G"} },
                new Row { {"Field1", "1 2 3 4 5 6 7"} },
                new Row { {"Field1", "    "}},
                new Row { {"Field1", null }}
            });
        }

        [Test]
        public void TestReplaceTransform()
        {
            var entity = new Entity();
            entity.All["Field1"] = new Field(FieldType.Field) { Transforms = new[] { new ReplaceTransform("b", "B"), new ReplaceTransform("2", "Two") }};

            var rows = TestOperation(
                _testInput.Object,
                new EntityTransform(entity),
                new LogOperation()
            );

            Assert.AreEqual("A B C d E f G", rows[0]["Field1"]);
            Assert.AreEqual("1 Two 3 4 5 6 7", rows[1]["Field1"]);

        }

        [Test]
        public void TestInsertTransform()
        {

            var entity = new Entity();
            entity.All["Field1"] = new Field(FieldType.Field) { Transforms = new[] { new InsertTransform(1, ".") } };

            var rows = TestOperation(
                _testInput.Object,
                new EntityDefaults(entity),
                new EntityTransform(entity),
                new LogOperation()
            );

            Assert.AreEqual(4, rows.Count);
            Assert.AreEqual("A. b C d E f G", rows[0]["Field1"]);
            Assert.AreEqual("1. 2 3 4 5 6 7", rows[1]["Field1"]);

        }

        [Test]
        public void TestRemoveTransform() {

            var entity = new Entity();
            entity.All["Field1"] = new Field(FieldType.Field) { Transforms = new[] { new RemoveTransform(2, 2) } };

            var rows = TestOperation(
                _testInput.Object,
                new EntityDefaults(entity),
                new EntityTransform(entity),
                new LogOperation()
            );

            Assert.AreEqual("A C d E f G", rows[0]["Field1"]);
            Assert.AreEqual("1 3 4 5 6 7", rows[1]["Field1"]);

        }

        [Test]
        public void TestTrimStartTransform() {

            var entity = new Entity();
            entity.All["Field1"] = new Field(FieldType.Field) {Transforms = new[] {new TrimStartTransform("1 ")}};
           
            var rows = TestOperation(
                _testInput.Object,
                new EntityTransform(entity),
                new LogOperation()
            );

            Assert.AreEqual("A b C d E f G", rows[0]["Field1"]);
            Assert.AreEqual("2 3 4 5 6 7", rows[1]["Field1"]);

        }

        [Test]
        public void TestTrimEndTransform1() {

            var entity = new Entity();
            entity.All["Field1"] = new Field(FieldType.Field) { Transforms = new[] { new TrimEndTransform(" ") }, Input = true };
            var fields = new Dictionary<string, Field> { { "", null } };

            var rows = TestOperation(
                _testInput.Object,
                new EntityTransform(entity),
                new LogOperation()
            );

            Assert.AreEqual("", rows[2]["Field1"]);

        }


        [Test]
        public void TestTrimEndTransform2() {

            var entity = new Entity();
            entity.All["Field1"] = new Field(FieldType.Field) { Transforms = new[] { new TrimEndTransform("G ") }, Input = true };
            var fields = new Dictionary<string, Field> { { "", null } };

            var rows = TestOperation(
                _testInput.Object,
                new EntityTransform(entity),
                new LogOperation()
            );

            Assert.AreEqual("A b C d E f", rows[0]["Field1"]);
            Assert.AreEqual("1 2 3 4 5 6 7", rows[1]["Field1"]);
            Assert.AreEqual("", rows[2]["Field1"]);

        }

        [Test]
        public void TestTrimTransform() {

            var entity = new Entity();
            entity.All["Field1"] = new Field(FieldType.Field) { Transforms = new[] { new TrimTransform("1G") }};

            var rows = TestOperation(
                _testInput.Object,
                new EntityTransform(entity),
                new LogOperation()
            );

            Assert.AreEqual("A b C d E f ", rows[0]["Field1"]);
            Assert.AreEqual(" 2 3 4 5 6 7", rows[1]["Field1"]);

        }

        [Test]
        public void TestSubStringTransform() {

            var entity = new Entity();
            entity.All["Field1"] = new Field(FieldType.Field) { Transforms = new[] { new SubstringTransform(4, 3) }, Input = true };
            var fields = new Dictionary<string, Field> { { "", null } };

            var rows = TestOperation(
                _testInput.Object,
                new EntityTransform(entity),
                new LogOperation()
            );

            Assert.AreEqual("C d", rows[0]["Field1"]);
            Assert.AreEqual("3 4", rows[1]["Field1"]);

        }

        [Test]
        public void TestLeftTransform() {

            var entity = new Entity();
            entity.All["Field1"] = new Field(FieldType.Field) { Transforms = new[] { new LeftTransform(4, null, null) }, Input = true };
            var fields = new Dictionary<string, Field> { { "", null } };

            var rows = TestOperation(
                _testInput.Object,
                new EntityTransform(entity),
                new LogOperation()
            );

            Assert.AreEqual("A b ", rows[0]["Field1"]);
            Assert.AreEqual("1 2 ", rows[1]["Field1"]);

        }

        [Test]
        public void TestRightTransform() {

            var entity = new Entity();
            entity.All["Field1"] = new Field(FieldType.Field) { Transforms = new[] { new RightTransform(3) }, Input = true };
            var fields = new Dictionary<string, Field> { { "", null } };

            var rows = TestOperation(
                _testInput.Object,
                new EntityTransform(entity),
                new LogOperation()
            );

            Assert.AreEqual("f G", rows[0]["Field1"]);
            Assert.AreEqual("6 7", rows[1]["Field1"]);

        }

        [Test]
        public void TestMapTransformStartsWith() {
            var mapEquals = new Dictionary<string, object>();
            var mapStartsWith = new Dictionary<string, object>();
            var mapEndsWith = new Dictionary<string, object>();

            mapEquals["A b C d E f G"] = "They're Just Letters!";
            mapStartsWith["1"] = "I used to start with 1.";

            var entity = new Entity();
            entity.All["Field1"] = new Field(FieldType.Field) { Input = true, Transforms = new[] { new MapTransform(new[] { mapEquals, mapStartsWith, mapEndsWith }, null, null) } };
            var fields = new Dictionary<string, Field> { { "", null } };

            var rows = TestOperation(
                _testInput.Object,
                new EntityTransform(entity),
                new LogOperation()
            );

            Assert.AreEqual("They're Just Letters!", rows[0]["Field1"]);
            Assert.AreEqual("I used to start with 1.", rows[1]["Field1"]);

        }

        [Test]
        public void TestMapTransformEndsWith() {
            var mapEquals = new Dictionary<string, object>();
            var mapStartsWith = new Dictionary<string, object>();
            var mapEndsWith = new Dictionary<string, object>();

            mapEquals["A b C d E f G"] = "They're Just Letters!";
            mapEndsWith["7"] = "I used to end with 7.";

            var entity = new Entity();
            entity.All["Field1"] = new Field(FieldType.Field) { Input = true, Transforms = new[] { new MapTransform(new[] { mapEquals, mapStartsWith, mapEndsWith }) } };
            var fields = new Dictionary<string, Field> { { "", null } };

            var rows = TestOperation(
                _testInput.Object,
                new EntityTransform(entity),
                new LogOperation()
            );

            Assert.AreEqual("They're Just Letters!", rows[0]["Field1"]);
            Assert.AreEqual("I used to end with 7.", rows[1]["Field1"]);

        }

        [Test]
        public void TestMapTransformMore() {
            var mapEquals = new Dictionary<string, object>();
            var mapStartsWith = new Dictionary<string, object>();
            var mapEndsWith = new Dictionary<string, object>();

            mapStartsWith["A b C"] = "abc";
            mapEndsWith["6 7"] = "67";

            var entity = new Entity();
            entity.All["Field1"] = new Field(FieldType.Field) { Input = true, Transforms = new[] { new MapTransform(new[] { mapEquals, mapStartsWith, mapEndsWith }) } };

            var rows = TestOperation(
                _testInput.Object,
                new EntityTransform(entity),
                new LogOperation()
            );

            Assert.AreEqual("abc", rows[0]["Field1"]);
            Assert.AreEqual("67", rows[1]["Field1"]);

        }

        [Test]
        public void TestJavscriptStringTransform() {

            var entity = new Entity();
            entity.All["Field1"] = new Field(FieldType.Field) { Input = true, Transforms = new[] { new JavascriptTransform("field.length;", null, null) } };

            var rows = TestOperation(
                _testInput.Object,
                new EntityDefaults(entity),
                new EntityTransform(entity),
                new LogOperation()
            );

            Assert.AreEqual("13", rows[0]["Field1"]);
            Assert.AreEqual("13", rows[1]["Field1"]);
            Assert.AreEqual("4", rows[2]["Field1"]);
            Assert.AreEqual("0", rows[3]["Field1"]);

        }

        [Test]
        public void TestJavscriptInt32Transform() {

            var numbersMock = new Mock<IOperation>();
            numbersMock.Setup(foo => foo.Execute(It.IsAny<IEnumerable<Row>>())).Returns(new List<Row> {
                new Row { {"Field1", 10} },
                new Row { {"Field1", 20} },
                new Row { {"Field1", 0}},
                new Row { {"Field1", null }}
            });
            var numbers = numbersMock.Object;

            var entity = new Entity();
            entity.All["Field1"] = new Field("System.Int32", 8, FieldType.Field, true, 0) { Input = true, Transforms = new[] { new JavascriptTransform("field * 2;", null, null) }, Default = 0 };
            var fields = new Dictionary<string, Field> { { "", null } };

            var rows = TestOperation(
                numbers,
                new EntityTransform(entity),
                new LogOperation()
            );

            Assert.AreEqual(20, rows[0]["Field1"]);
            Assert.AreEqual(40, rows[1]["Field1"]);
            Assert.AreEqual(0, rows[2]["Field1"]);
            Assert.AreEqual(0, rows[3]["Field1"]);
        }

        [Test]
        public void TestPadLeftTransform() {

            var mock = new Mock<IOperation>();
            mock.Setup(foo => foo.Execute(It.IsAny<IEnumerable<Row>>())).Returns(new List<Row> {
                new Row { {"Field1", "345"} },
                new Row { {"Field1", ""} },
                new Row { {"Field1", "x"}},
                new Row { {"Field1", null }}
            });
            var input = mock.Object;

            var entity = new Entity();
            entity.All["Field1"] = new Field(FieldType.Field) { Input = true, Transforms = new[] { new PadLeftTransform(5, '0') }, Default = "00000" };
            var fields = new Dictionary<string, Field> { { "", null } };

            var rows = TestOperation(
                input,
                new EntityTransform(entity),
                new LogOperation()
            );

            Assert.AreEqual("00345", rows[0]["Field1"]);
            Assert.AreEqual("00000", rows[1]["Field1"]);
            Assert.AreEqual("0000x", rows[2]["Field1"]);
            Assert.AreEqual("00000", rows[3]["Field1"]);
        }

        [Test]
        public void TestPadRightTransform() {

            var mock = new Mock<IOperation>();
            mock.Setup(foo => foo.Execute(It.IsAny<IEnumerable<Row>>())).Returns(new List<Row> {
                new Row { {"Field1", "345"} },
                new Row { {"Field1", ""} },
                new Row { {"Field1", "x"}},
                new Row { {"Field1", null }}
            });
            var input = mock.Object;

            var entity = new Entity();
            entity.All["Field1"] = new Field(FieldType.Field) { Input = true, Transforms = new[] { new PadRightTransform(5, '0') }, Default = "00000" };
            var fields = new Dictionary<string, Field> { { "", null } };

            var rows = TestOperation(
                input,
                new EntityTransform(entity),
                new LogOperation()
            );

            Assert.AreEqual("34500", rows[0]["Field1"]);
            Assert.AreEqual("00000", rows[1]["Field1"]);
            Assert.AreEqual("x0000", rows[2]["Field1"]);
            Assert.AreEqual("00000", rows[3]["Field1"]);
        }


    }
}