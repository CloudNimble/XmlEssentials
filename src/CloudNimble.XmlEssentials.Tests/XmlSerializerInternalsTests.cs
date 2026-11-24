using System;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Tests.POCOs;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Checks inside the serializer that no document or type reaches: each guards a precondition the callers keep.
    /// </summary>
    [TestClass]
    public class XmlSerializerInternalsTests
    {

        #region Public Methods

        [TestMethod]
        public void TryCreateModel_ForAMemberThatIsIgnored_CreatesNone()
        {
            // CreateModels sets ignored members aside before it asks for a model, because an ignored member still
            // hides the one it overrides. The rule for one member stands on its own all the same.
            var ignored = new XmlMemberFacts(typeof(CamelMarkedCtor).GetProperty(nameof(CamelMarkedCtor.CreatedBy))!);
            XmlObjectBinder.IsIgnored(ignored).Should().BeTrue();
            XmlObjectBinder.TryCreateModel(ignored, new XmlSerializerOptions(), 0, out var model).Should().BeFalse();
            model.Member.Should().BeNull();
        }

        [TestMethod]
        public void ConstructorFacts_FromAConstructorWithAnUnnamedParameter_KeepTheNameNull()
        {
            // An array constructor is the one the runtime gives parameters with no names. A parameter with no
            // name fills no member, so nothing is asked about it.
            var constructor = typeof(int[]).GetConstructor([typeof(int)])!;
            constructor.GetParameters()[0].Name.Should().BeNull();
            var facts = XmlConstructorFacts.FromReflection(constructor, static name => throw new InvalidOperationException("Asked about '" + name + "'."));
            facts.Parameters.Should().ContainSingle();
            facts.Parameters[0].Name.Should().BeNull();
            facts.Parameters[0].ParameterType.Should().Be(typeof(int));
            facts.Parameters[0].IsIgnoredMember.Should().BeFalse();
            facts.Parameters[0].HasDefaultValue.Should().BeFalse();
            facts.Invoke([3, "ignored"]).Should().BeOfType<int[]>().Which.Should().HaveCount(3);
            XmlObjectBinder.MapConstructor(facts, []).Should().Equal(-1);
        }

        [TestMethod]
        public void Populate_OnAConverterThatCannotPopulate_IsRejected()
        {
            // The binder asks CanPopulate before a member is populated, so only a caller that skips the question gets here.
            var options = new XmlSerializerOptions();
            var converter = options.GetConverter(typeof(int));
            converter.CanPopulate.Should().BeFalse();
            options.GetConverter(typeof(PopulateChild)).CanPopulate.Should().BeTrue();

            var threw = false;
            var reader = new Utf8XmlReader("<int>1</int>"u8);
            reader.Read();
            try
            {
                converter.Populate(ref reader, 1, options);
            }
            catch (NotSupportedException ex)
            {
                threw = true;
                ex.Message.Should().Be("The converter '" + converter.GetType() + "' cannot populate an existing value.");
            }

            threw.Should().BeTrue();
        }

        #endregion

    }

}
