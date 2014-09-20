/*
 * Copyright (C) 2014 The Libphonenumber Authors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */


using System;
using System.Xml;
using Google.PhoneNumbers.GenerateData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Google.PhoneNumbers.Test
{
    [TestClass]
    public class BuildMetadataFromXmlTest
    {
        private static XmlElement parseXmlString(String xmlString)
        {
            var xml = new XmlDocument();
            xml.LoadXml(xmlString);
            return xml.DocumentElement;
        }

        // Tests validateRE().
        [TestMethod] public void testValidateRERemovesWhiteSpaces()
        {
            String input = " hello world ";
            // Should remove all the white spaces contained in the provided string.
            Assert.AreEqual("helloworld", BuildMetadataFromXml.validateRE(input, true));
            // Make sure it only happens when the last parameter is set to true.
            Assert.AreEqual(" hello world ", BuildMetadataFromXml.validateRE(input, false));
        }

        [TestMethod] public void testValidateREThrowsException()
        {
            String invalidPattern = "[";
            // Should throw an exception when an invalid pattern is provided independently of the last
            // parameter (remove white spaces).
            try
            {
                BuildMetadataFromXml.validateRE(invalidPattern, false);
                Assert.Fail();
            }
            catch (ArgumentException)
            {
                // Test passed.
            }
            try
            {
                BuildMetadataFromXml.validateRE(invalidPattern, true);
                Assert.Fail();
            }
            catch (ArgumentException)
            {
                // Test passed.
            }
            // We don't allow | to be followed by ) because it introduces bugs, since we typically use it at
            // the end of each line and when a line is deleted, if the pipe from the previous line is not
            // removed, we end up erroneously accepting an empty group as well.
            String patternWithPipeFollowedByClosingParentheses = "|)";
            try
            {
                BuildMetadataFromXml.validateRE(patternWithPipeFollowedByClosingParentheses, true);
                Assert.Fail();
            }
            catch (ArgumentException)
            {
                // Test passed.
            }
            String patternWithPipeFollowedByNewLineAndClosingParentheses = "|\n)";
            try
            {
                BuildMetadataFromXml.validateRE(patternWithPipeFollowedByNewLineAndClosingParentheses, true);
                Assert.Fail();
            }
            catch (ArgumentException)
            {
                // Test passed.
            }
        }

        [TestMethod] public void testValidateRE()
        {
            String validPattern = "[a-zA-Z]d{1,9}";
            // The provided pattern should be left unchanged.
            Assert.AreEqual(validPattern, BuildMetadataFromXml.validateRE(validPattern, false));
        }

        // Tests getNationalPrefix().
        [TestMethod] public void testGetNationalPrefix()
        {
            String xmlInput = "<territory nationalPrefix='00'/>";
            XmlElement territoryElement = parseXmlString(xmlInput);
            Assert.AreEqual("00", BuildMetadataFromXml.getNationalPrefix(territoryElement));
        }

        // Tests loadTerritoryTagMetadata().
        [TestMethod] public void testLoadTerritoryTagMetadata()
        {
            String xmlInput =
                "<territory countryCode='33' leadingDigits='2' internationalPrefix='00'" +
                "           preferredInternationalPrefix='0011' nationalPrefixForParsing='0'" +
                "           nationalPrefixTransformRule='9$1'" + // nationalPrefix manually injected.
                "           preferredExtnPrefix=' x' mainCountryForCode='true'" +
                "           leadingZeroPossible='true' mobileNumberPortableRegion='true'>" +
                "</territory>";
            XmlElement territoryElement = parseXmlString(xmlInput);
            PhoneMetadata.Builder phoneMetadata =
                BuildMetadataFromXml.loadTerritoryTagMetadata("33", territoryElement, "0");
            Assert.AreEqual(33, phoneMetadata.getCountryCode());
            Assert.AreEqual("2", phoneMetadata.getLeadingDigits());
            Assert.AreEqual("00", phoneMetadata.getInternationalPrefix());
            Assert.AreEqual("0011", phoneMetadata.getPreferredInternationalPrefix());
            Assert.AreEqual("0", phoneMetadata.getNationalPrefixForParsing());
            Assert.AreEqual("9$1", phoneMetadata.getNationalPrefixTransformRule());
            Assert.AreEqual("0", phoneMetadata.getNationalPrefix());
            Assert.AreEqual(" x", phoneMetadata.getPreferredExtnPrefix());
            Assert.IsTrue(phoneMetadata.getMainCountryForCode());
            Assert.IsTrue(phoneMetadata.isLeadingZeroPossible());
            Assert.IsTrue(phoneMetadata.isMobileNumberPortableRegion());
        }

        [TestMethod] public void testLoadTerritoryTagMetadataSetsBooleanFieldsToFalseByDefault()
        {
            String xmlInput = "<territory countryCode='33'/>";
            XmlElement territoryElement = parseXmlString(xmlInput);
            PhoneMetadata.Builder phoneMetadata =
                BuildMetadataFromXml.loadTerritoryTagMetadata("33", territoryElement, "");
            Assert.IsFalse(phoneMetadata.getMainCountryForCode());
            Assert.IsFalse(phoneMetadata.isLeadingZeroPossible());
            Assert.IsFalse(phoneMetadata.isMobileNumberPortableRegion());
        }

        [TestMethod] public void testLoadTerritoryTagMetadataSetsNationalPrefixForParsingByDefault()
        {
            String xmlInput = "<territory countryCode='33'/>";
            XmlElement territoryElement = parseXmlString(xmlInput);
            PhoneMetadata.Builder phoneMetadata =
                BuildMetadataFromXml.loadTerritoryTagMetadata("33", territoryElement, "00");
            // When unspecified, nationalPrefixForParsing defaults to nationalPrefix.
            Assert.AreEqual("00", phoneMetadata.getNationalPrefix());
            Assert.AreEqual(phoneMetadata.getNationalPrefix(), phoneMetadata.getNationalPrefixForParsing());
        }

        [TestMethod] public void testLoadTerritoryTagMetadataWithRequiredAttributesOnly()
        {
            String xmlInput = "<territory countryCode='33' internationalPrefix='00'/>";
            XmlElement territoryElement = parseXmlString(xmlInput);
            // Should not throw any exception.
            BuildMetadataFromXml.loadTerritoryTagMetadata("33", territoryElement, "");
        }

        // Tests loadInternationalFormat().
        [TestMethod] public void testLoadInternationalFormat()
        {
            String intlFormat = "$1 $2";
            String xmlInput = "<numberFormat><intlFormat>" + intlFormat + "</intlFormat></numberFormat>";
            XmlElement numberFormatElement = parseXmlString(xmlInput);
            PhoneMetadata.Builder metadata = PhoneMetadata.newBuilder();
            NumberFormat nationalFormat = NumberFormat.newBuilder().build();

            Assert.IsTrue(BuildMetadataFromXml.loadInternationalFormat(metadata, numberFormatElement,
                nationalFormat));
            Assert.AreEqual(intlFormat, metadata.getIntlNumberFormat(0).getFormat());
        }

        [TestMethod] public void testLoadInternationalFormatWithBothNationalAndIntlFormatsDefined()
        {
            String intlFormat = "$1 $2";
            String xmlInput = "<numberFormat><intlFormat>" + intlFormat + "</intlFormat></numberFormat>";
            XmlElement numberFormatElement = parseXmlString(xmlInput);
            PhoneMetadata.Builder metadata = PhoneMetadata.newBuilder();
            NumberFormat.Builder nationalFormat = NumberFormat.newBuilder();
            nationalFormat.setFormat("$1");

            Assert.IsTrue(BuildMetadataFromXml.loadInternationalFormat(metadata, numberFormatElement,
                nationalFormat.build()));
            Assert.AreEqual(intlFormat, metadata.getIntlNumberFormat(0).getFormat());
        }

        [TestMethod] public void testLoadInternationalFormatExpectsOnlyOnePattern()
        {
            String xmlInput = "<numberFormat><intlFormat/><intlFormat/></numberFormat>";
            XmlElement numberFormatElement = parseXmlString(xmlInput);
            PhoneMetadata.Builder metadata = PhoneMetadata.newBuilder();

            // Should throw an exception as multiple intlFormats are provided.
            try
            {
                BuildMetadataFromXml.loadInternationalFormat(metadata, numberFormatElement,
                    NumberFormat.newBuilder().build());
                Assert.Fail();
            }
            catch (Exception)
            {
                // Test passed.
            }
        }

        [TestMethod] public void testLoadInternationalFormatUsesNationalFormatByDefault()
        {
            String xmlInput = "<numberFormat></numberFormat>";
            XmlElement numberFormatElement = parseXmlString(xmlInput);
            PhoneMetadata.Builder metadata = PhoneMetadata.newBuilder();
            NumberFormat.Builder nationalFormat = NumberFormat.newBuilder();
            String nationalPattern = "$1 $2 $3";
            nationalFormat.setFormat(nationalPattern);

            Assert.IsFalse(BuildMetadataFromXml.loadInternationalFormat(metadata, numberFormatElement,
                nationalFormat.build()));
            Assert.AreEqual(nationalPattern, metadata.getIntlNumberFormat(0).getFormat());
        }

        [TestMethod] public void testLoadInternationalFormatCopiesNationalFormatData()
        {
            String xmlInput = "<numberFormat></numberFormat>";
            XmlElement numberFormatElement = parseXmlString(xmlInput);
            PhoneMetadata.Builder metadata = PhoneMetadata.newBuilder();
            NumberFormat.Builder nationalFormat = NumberFormat.newBuilder();
            nationalFormat.setFormat("$1-$2");
            nationalFormat.setNationalPrefixOptionalWhenFormatting(true);

            Assert.IsFalse(BuildMetadataFromXml.loadInternationalFormat(metadata, numberFormatElement,
                nationalFormat.build()));
            Assert.IsTrue(metadata.getIntlNumberFormat(0).isNationalPrefixOptionalWhenFormatting());
        }

        [TestMethod] public void testLoadNationalFormat()
        {
            String nationalFormat = "$1 $2";
            String xmlInput = String.Format("<numberFormat><format>{0}</format></numberFormat>",
                nationalFormat);
            XmlElement numberFormatElement = parseXmlString(xmlInput);
            PhoneMetadata.Builder metadata = PhoneMetadata.newBuilder();
            NumberFormat.Builder numberFormat = NumberFormat.newBuilder();
            BuildMetadataFromXml.loadNationalFormat(metadata, numberFormatElement, numberFormat);
            Assert.AreEqual(nationalFormat, numberFormat.getFormat());
        }

        [TestMethod] public void testLoadNationalFormatRequiresFormat()
        {
            String xmlInput = "<numberFormat></numberFormat>";
            XmlElement numberFormatElement = parseXmlString(xmlInput);
            PhoneMetadata.Builder metadata = PhoneMetadata.newBuilder();
            NumberFormat.Builder numberFormat = NumberFormat.newBuilder();

            try
            {
                BuildMetadataFromXml.loadNationalFormat(metadata, numberFormatElement, numberFormat);
                Assert.Fail();
            }
            catch (Exception)
            {
                // Test passed.
            }
        }

        [TestMethod] public void testLoadNationalFormatExpectsExactlyOneFormat()
        {
            String xmlInput = "<numberFormat><format/><format/></numberFormat>";
            XmlElement numberFormatElement = parseXmlString(xmlInput);
            PhoneMetadata.Builder metadata = PhoneMetadata.newBuilder();
            NumberFormat.Builder numberFormat = NumberFormat.newBuilder();

            try
            {
                BuildMetadataFromXml.loadNationalFormat(metadata, numberFormatElement, numberFormat);
                Assert.Fail();
            }
            catch (Exception)
            {
                // Test passed.
            }
        }

        // Tests loadAvailableFormats().
        [TestMethod] public void testLoadAvailableFormats()
        {
            String xmlInput =
                "<territory >" +
                "  <availableFormats>" +
                "    <numberFormat nationalPrefixFormattingRule='($FG)'" +
                "                  carrierCodeFormattingRule='$NP $CC ($FG)'>" +
                "      <format>$1 $2 $3</format>" +
                "    </numberFormat>" +
                "  </availableFormats>" +
                "</territory>";
            XmlElement element = parseXmlString(xmlInput);
            PhoneMetadata.Builder metadata = PhoneMetadata.newBuilder();
            BuildMetadataFromXml.loadAvailableFormats(
                metadata, element, "0", "", false /* NP not optional */);
            Assert.AreEqual("($1)", metadata.getNumberFormat(0).getNationalPrefixFormattingRule());
            Assert.AreEqual("0 $CC ($1)", metadata.getNumberFormat(0).getDomesticCarrierCodeFormattingRule());
            Assert.AreEqual("$1 $2 $3", metadata.getNumberFormat(0).getFormat());
        }

        [TestMethod] public void testLoadAvailableFormatsPropagatesCarrierCodeFormattingRule()
        {
            String xmlInput =
                "<territory carrierCodeFormattingRule='$NP $CC ($FG)'>" +
                "  <availableFormats>" +
                "    <numberFormat nationalPrefixFormattingRule='($FG)'>" +
                "      <format>$1 $2 $3</format>" +
                "    </numberFormat>" +
                "  </availableFormats>" +
                "</territory>";
            XmlElement element = parseXmlString(xmlInput);
            PhoneMetadata.Builder metadata = PhoneMetadata.newBuilder();
            BuildMetadataFromXml.loadAvailableFormats(
                metadata, element, "0", "", false /* NP not optional */);
            Assert.AreEqual("($1)", metadata.getNumberFormat(0).getNationalPrefixFormattingRule());
            Assert.AreEqual("0 $CC ($1)", metadata.getNumberFormat(0).getDomesticCarrierCodeFormattingRule());
            Assert.AreEqual("$1 $2 $3", metadata.getNumberFormat(0).getFormat());
        }

        [TestMethod] public void testLoadAvailableFormatsSetsProvidedNationalPrefixFormattingRule()
        {
            String xmlInput =
                "<territory>" +
                "  <availableFormats>" +
                "    <numberFormat><format>$1 $2 $3</format></numberFormat>" +
                "  </availableFormats>" +
                "</territory>";
            XmlElement element = parseXmlString(xmlInput);
            PhoneMetadata.Builder metadata = PhoneMetadata.newBuilder();
            BuildMetadataFromXml.loadAvailableFormats(
                metadata, element, "", "($1)", false /* NP not optional */);
            Assert.AreEqual("($1)", metadata.getNumberFormat(0).getNationalPrefixFormattingRule());
        }

        [TestMethod] public void testLoadAvailableFormatsClearsIntlFormat()
        {
            String xmlInput =
                "<territory>" +
                "  <availableFormats>" +
                "    <numberFormat><format>$1 $2 $3</format></numberFormat>" +
                "  </availableFormats>" +
                "</territory>";
            XmlElement element = parseXmlString(xmlInput);
            PhoneMetadata.Builder metadata = PhoneMetadata.newBuilder();
            BuildMetadataFromXml.loadAvailableFormats(
                metadata, element, "0", "($1)", false /* NP not optional */);
            Assert.AreEqual(0, metadata.IntlNumberFormatSize());
        }

        [TestMethod] public void testLoadAvailableFormatsHandlesMultipleNumberFormats()
        {
            String xmlInput =
                "<territory>" +
                "  <availableFormats>" +
                "    <numberFormat><format>$1 $2 $3</format></numberFormat>" +
                "    <numberFormat><format>$1-$2</format></numberFormat>" +
                "  </availableFormats>" +
                "</territory>";
            XmlElement element = parseXmlString(xmlInput);
            PhoneMetadata.Builder metadata = PhoneMetadata.newBuilder();
            BuildMetadataFromXml.loadAvailableFormats(
                metadata, element, "0", "($1)", false /* NP not optional */);
            Assert.AreEqual("$1 $2 $3", metadata.getNumberFormat(0).getFormat());
            Assert.AreEqual("$1-$2", metadata.getNumberFormat(1).getFormat());
        }

        [TestMethod] public void testLoadInternationalFormatDoesNotSetIntlFormatWhenNA()
        {
            String xmlInput = "<numberFormat><intlFormat>NA</intlFormat></numberFormat>";
            XmlElement numberFormatElement = parseXmlString(xmlInput);
            PhoneMetadata.Builder metadata = PhoneMetadata.newBuilder();
            NumberFormat.Builder nationalFormat = NumberFormat.newBuilder();
            nationalFormat.setFormat("$1 $2");

            BuildMetadataFromXml.loadInternationalFormat(metadata, numberFormatElement,
                nationalFormat.build());
            Assert.AreEqual(0, metadata.IntlNumberFormatSize());
        }

        // Tests setLeadingDigitsPatterns() in the case of international and national formatting rules
        // being present but not both defined for this numberFormat - we don't want to add them twice.
        [TestMethod] public void testSetLeadingDigitsPatternsNotAddedTwiceWhenInternationalFormatsPresent()
        {
            String xmlInput =
                "  <availableFormats>" +
                "    <numberFormat pattern=\"(1)(\\d{3})\">" +
                "      <leadingDigits>1</leadingDigits>" +
                "      <format>$1</format>" +
                "    </numberFormat>" +
                "    <numberFormat pattern=\"(2)(\\d{3})\">" +
                "      <leadingDigits>2</leadingDigits>" +
                "      <format>$1</format>" +
                "      <intlFormat>9-$1</intlFormat>" +
                "    </numberFormat>" +
                "  </availableFormats>";
            XmlElement element = parseXmlString(xmlInput);
            PhoneMetadata.Builder metadata = PhoneMetadata.newBuilder();
            BuildMetadataFromXml.loadAvailableFormats(
                metadata, element, "0", "", false /* NP not optional */);
            Assert.AreEqual(1, metadata.getNumberFormat(0).leadingDigitsPatternSize());
            Assert.AreEqual(1, metadata.getNumberFormat(1).leadingDigitsPatternSize());
            // When we merge the national format rules into the international format rules, we shouldn't add
            // the leading digit patterns multiple times.
            Assert.AreEqual(1, metadata.getIntlNumberFormat(0).leadingDigitsPatternSize());
            Assert.AreEqual(1, metadata.getIntlNumberFormat(1).leadingDigitsPatternSize());
        }

        // Tests setLeadingDigitsPatterns().
        [TestMethod] public void testSetLeadingDigitsPatterns()
        {
            String xmlInput =
                "<numberFormat>" +
                "<leadingDigits>1</leadingDigits><leadingDigits>2</leadingDigits>" +
                "</numberFormat>";
            XmlElement numberFormatElement = parseXmlString(xmlInput);
            NumberFormat.Builder numberFormat = NumberFormat.newBuilder();
            BuildMetadataFromXml.setLeadingDigitsPatterns(numberFormatElement, numberFormat);

            Assert.AreEqual("1", numberFormat.getLeadingDigitsPattern(0));
            Assert.AreEqual("2", numberFormat.getLeadingDigitsPattern(1));
        }

        // Tests getNationalPrefixFormattingRuleFromElement().
        [TestMethod] public void testGetNationalPrefixFormattingRuleFromElement()
        {
            String xmlInput = "<territory nationalPrefixFormattingRule='$NP$FG'/>";
            XmlElement element = parseXmlString(xmlInput);
            Assert.AreEqual("0$1",
                BuildMetadataFromXml.getNationalPrefixFormattingRuleFromXmlElement(element, "0"));
        }

        // Tests getDomesticCarrierCodeFormattingRuleFromElement().
        [TestMethod] public void testGetDomesticCarrierCodeFormattingRuleFromElement()
        {
            String xmlInput = "<territory carrierCodeFormattingRule='$NP$CC $FG'/>";
            XmlElement element = parseXmlString(xmlInput);
            Assert.AreEqual("0$CC $1",
                BuildMetadataFromXml.getDomesticCarrierCodeFormattingRuleFromXmlElement(element,
                    "0"));
        }

        // Tests isValidNumberType().
        [TestMethod] public void testIsValidNumberTypeWithInvalidInput()
        {
            Assert.IsFalse(BuildMetadataFromXml.isValidNumberType("invalidType"));
        }

        // Tests processPhoneNumberDescElement().
        [TestMethod] public void testProcessPhoneNumberDescElementWithInvalidInput()
        {
            PhoneNumberDesc.Builder generalDesc = PhoneNumberDesc.newBuilder();
            XmlElement territoryElement = parseXmlString("<territory/>");
            PhoneNumberDesc.Builder phoneNumberDesc;

            phoneNumberDesc = BuildMetadataFromXml.processPhoneNumberDescElement(
                generalDesc, territoryElement, "invalidType", false);
            Assert.AreEqual("NA", phoneNumberDesc.getPossibleNumberPattern());
            Assert.AreEqual("NA", phoneNumberDesc.getNationalNumberPattern());
        }

        [TestMethod] public void testProcessPhoneNumberDescElementMergesWithGeneralDesc()
        {
            PhoneNumberDesc.Builder generalDesc = PhoneNumberDesc.newBuilder();
            generalDesc.setPossibleNumberPattern("\\d{6}");
            XmlElement territoryElement = parseXmlString("<territory><fixedLine/></territory>");
            PhoneNumberDesc.Builder phoneNumberDesc;

            phoneNumberDesc = BuildMetadataFromXml.processPhoneNumberDescElement(
                generalDesc, territoryElement, "fixedLine", false);
            Assert.AreEqual("\\d{6}", phoneNumberDesc.getPossibleNumberPattern());
        }

        [TestMethod] public void testProcessPhoneNumberDescElementOverridesGeneralDesc()
        {
            PhoneNumberDesc.Builder generalDesc = PhoneNumberDesc.newBuilder();
            generalDesc.setPossibleNumberPattern("\\d{8}");
            String xmlInput =
                "<territory><fixedLine>" +
                "  <possibleNumberPattern>\\d{6}</possibleNumberPattern>" +
                "</fixedLine></territory>";
            XmlElement territoryElement = parseXmlString(xmlInput);
            PhoneNumberDesc.Builder phoneNumberDesc;

            phoneNumberDesc = BuildMetadataFromXml.processPhoneNumberDescElement(
                generalDesc, territoryElement, "fixedLine", false);
            Assert.AreEqual("\\d{6}", phoneNumberDesc.getPossibleNumberPattern());
        }

        [TestMethod] public void testProcessPhoneNumberDescElementHandlesLiteBuild()
        {
            PhoneNumberDesc.Builder generalDesc = PhoneNumberDesc.newBuilder();
            String xmlInput =
                "<territory><fixedLine>" +
                "  <exampleNumber>01 01 01 01</exampleNumber>" +
                "</fixedLine></territory>";
            XmlElement territoryElement = parseXmlString(xmlInput);
            PhoneNumberDesc.Builder phoneNumberDesc;

            phoneNumberDesc = BuildMetadataFromXml.processPhoneNumberDescElement(
                generalDesc, territoryElement, "fixedLine", true);
            Assert.AreEqual("", phoneNumberDesc.getExampleNumber());
        }

        [TestMethod] public void testProcessPhoneNumberDescOutputsExampleNumberByDefault()
        {
            PhoneNumberDesc.Builder generalDesc = PhoneNumberDesc.newBuilder();
            String xmlInput =
                "<territory><fixedLine>" +
                "  <exampleNumber>01 01 01 01</exampleNumber>" +
                "</fixedLine></territory>";
            XmlElement territoryElement = parseXmlString(xmlInput);
            PhoneNumberDesc.Builder phoneNumberDesc;

            phoneNumberDesc = BuildMetadataFromXml.processPhoneNumberDescElement(
                generalDesc, territoryElement, "fixedLine", false);
            Assert.AreEqual("01 01 01 01", phoneNumberDesc.getExampleNumber());
        }

        [TestMethod] public void testProcessPhoneNumberDescRemovesWhiteSpacesInPatterns()
        {
            PhoneNumberDesc.Builder generalDesc = PhoneNumberDesc.newBuilder();
            String xmlInput =
                "<territory><fixedLine>" +
                "  <possibleNumberPattern>\t \\d { 6 } </possibleNumberPattern>" +
                "</fixedLine></territory>";
            XmlElement countryElement = parseXmlString(xmlInput);
            PhoneNumberDesc.Builder phoneNumberDesc;

            phoneNumberDesc = BuildMetadataFromXml.processPhoneNumberDescElement(
                generalDesc, countryElement, "fixedLine", false);
            Assert.AreEqual("\\d{6}", phoneNumberDesc.getPossibleNumberPattern());
        }

        // Tests setRelevantDescPatterns().
        [TestMethod] public void testSetRelevantDescPatternsSetsSameMobileAndFixedLinePattern()
        {
            String xmlInput =
                "<territory countryCode=\"33\">" +
                "  <fixedLine><nationalNumberPattern>\\d{6}</nationalNumberPattern></fixedLine>" +
                "  <mobile><nationalNumberPattern>\\d{6}</nationalNumberPattern></mobile>" +
                "</territory>";
            XmlElement territoryElement = parseXmlString(xmlInput);
            PhoneMetadata.Builder metadata = PhoneMetadata.newBuilder();
            // Should set sameMobileAndFixedPattern to true.
            BuildMetadataFromXml.setRelevantDescPatterns(metadata, territoryElement, false /* liteBuild */,
                false /* isShortNumberMetadata */);
            Assert.IsTrue(metadata.isSameMobileAndFixedLinePattern());
        }

        [TestMethod] public void testSetRelevantDescPatternsSetsAllDescriptionsForRegularLengthNumbers()
        {
            String xmlInput =
                "<territory countryCode=\"33\">" +
                "  <fixedLine><nationalNumberPattern>\\d{1}</nationalNumberPattern></fixedLine>" +
                "  <mobile><nationalNumberPattern>\\d{2}</nationalNumberPattern></mobile>" +
                "  <pager><nationalNumberPattern>\\d{3}</nationalNumberPattern></pager>" +
                "  <tollFree><nationalNumberPattern>\\d{4}</nationalNumberPattern></tollFree>" +
                "  <premiumRate><nationalNumberPattern>\\d{5}</nationalNumberPattern></premiumRate>" +
                "  <sharedCost><nationalNumberPattern>\\d{6}</nationalNumberPattern></sharedCost>" +
                "  <personalNumber><nationalNumberPattern>\\d{7}</nationalNumberPattern></personalNumber>" +
                "  <voip><nationalNumberPattern>\\d{8}</nationalNumberPattern></voip>" +
                "  <uan><nationalNumberPattern>\\d{9}</nationalNumberPattern></uan>" +
                "</territory>";
            XmlElement territoryElement = parseXmlString(xmlInput);
            PhoneMetadata.Builder metadata = PhoneMetadata.newBuilder();
            BuildMetadataFromXml.setRelevantDescPatterns(metadata, territoryElement, false /* liteBuild */,
                false /* isShortNumberMetadata */);
            Assert.AreEqual("\\d{1}", metadata.getFixedLine().getNationalNumberPattern());
            Assert.AreEqual("\\d{2}", metadata.getMobile().getNationalNumberPattern());
            Assert.AreEqual("\\d{3}", metadata.getPager().getNationalNumberPattern());
            Assert.AreEqual("\\d{4}", metadata.getTollFree().getNationalNumberPattern());
            Assert.AreEqual("\\d{5}", metadata.getPremiumRate().getNationalNumberPattern());
            Assert.AreEqual("\\d{6}", metadata.getSharedCost().getNationalNumberPattern());
            Assert.AreEqual("\\d{7}", metadata.getPersonalNumber().getNationalNumberPattern());
            Assert.AreEqual("\\d{8}", metadata.getVoip().getNationalNumberPattern());
            Assert.AreEqual("\\d{9}", metadata.getUan().getNationalNumberPattern());
        }

        [TestMethod] public void testSetRelevantDescPatternsSetsAllDescriptionsForShortNumbers()
        {
            String xmlInput =
                "<territory ID=\"FR\">" +
                "  <tollFree><nationalNumberPattern>\\d{1}</nationalNumberPattern></tollFree>" +
                "  <standardRate><nationalNumberPattern>\\d{2}</nationalNumberPattern></standardRate>" +
                "  <premiumRate><nationalNumberPattern>\\d{3}</nationalNumberPattern></premiumRate>" +
                "  <shortCode><nationalNumberPattern>\\d{4}</nationalNumberPattern></shortCode>" +
                "  <carrierSpecific>" +
                "    <nationalNumberPattern>\\d{5}</nationalNumberPattern>" +
                "  </carrierSpecific>" +
                "</territory>";
            XmlElement territoryElement = parseXmlString(xmlInput);
            PhoneMetadata.Builder metadata = PhoneMetadata.newBuilder();
            BuildMetadataFromXml.setRelevantDescPatterns(metadata, territoryElement, false /* liteBuild */,
                true /* isShortNumberMetadata */);
            Assert.AreEqual("\\d{1}", metadata.getTollFree().getNationalNumberPattern());
            Assert.AreEqual("\\d{2}", metadata.getStandardRate().getNationalNumberPattern());
            Assert.AreEqual("\\d{3}", metadata.getPremiumRate().getNationalNumberPattern());
            Assert.AreEqual("\\d{4}", metadata.getShortCode().getNationalNumberPattern());
            Assert.AreEqual("\\d{5}", metadata.getCarrierSpecific().getNationalNumberPattern());
        }

        [TestMethod] public void testAlternateFormatsOmitsDescPatterns()
        {
            String xmlInput =
                "<territory countryCode=\"33\">" +
                "  <availableFormats>" +
                "    <numberFormat pattern=\"(1)(\\d{3})\">" +
                "      <leadingDigits>1</leadingDigits>" +
                "      <format>$1</format>" +
                "    </numberFormat>" +
                "  </availableFormats>" +
                "  <fixedLine><nationalNumberPattern>\\d{1}</nationalNumberPattern></fixedLine>" +
                "  <shortCode><nationalNumberPattern>\\d{2}</nationalNumberPattern></shortCode>" +
                "</territory>";
            XmlElement territoryElement = parseXmlString(xmlInput);
            PhoneMetadata metadata = BuildMetadataFromXml.loadCountryMetadata("FR", territoryElement,
                false /* liteBuild */, false /* isShortNumberMetadata */,
                true /* isAlternateFormatsMetadata */);
            Assert.AreEqual("(1)(\\d{3})", metadata.getNumberFormat(0).getPattern());
            Assert.AreEqual("1", metadata.getNumberFormat(0).getLeadingDigitsPattern(0));
            Assert.AreEqual("$1", metadata.getNumberFormat(0).getFormat());
            Assert.IsFalse(metadata.HasFixedLine());
            Assert.IsNull(metadata.getFixedLine());
            Assert.IsFalse(metadata.HasShortCode());
            Assert.IsNull(metadata.getShortCode());
        }

        [TestMethod] public void testNationalPrefixRulesSetCorrectly()
        {
            String xmlInput =
                "<territory countryCode=\"33\" nationalPrefix=\"0\"" +
                " nationalPrefixFormattingRule=\"$NP$FG\">" +
                "  <availableFormats>" +
                "    <numberFormat pattern=\"(1)(\\d{3})\" nationalPrefixOptionalWhenFormatting=\"true\">" +
                "      <leadingDigits>1</leadingDigits>" +
                "      <format>$1</format>" +
                "    </numberFormat>" +
                "    <numberFormat pattern=\"(\\d{3})\" nationalPrefixOptionalWhenFormatting=\"false\">" +
                "      <leadingDigits>2</leadingDigits>" +
                "      <format>$1</format>" +
                "    </numberFormat>" +
                "  </availableFormats>" +
                "  <fixedLine><nationalNumberPattern>\\d{1}</nationalNumberPattern></fixedLine>" +
                "</territory>";
            XmlElement territoryElement = parseXmlString(xmlInput);
            PhoneMetadata metadata = BuildMetadataFromXml.loadCountryMetadata("FR", territoryElement,
                false /* liteBuild */, false /* isShortNumberMetadata */,
                true /* isAlternateFormatsMetadata */);
            Assert.IsTrue(metadata.getNumberFormat(0).isNationalPrefixOptionalWhenFormatting());
            // This is inherited from the territory, with $NP replaced by the actual national prefix, and
            // $FG replaced with $1.
            Assert.AreEqual("0$1", metadata.getNumberFormat(0).getNationalPrefixFormattingRule());
            // Here it is explicitly set to false.
            Assert.IsFalse(metadata.getNumberFormat(1).isNationalPrefixOptionalWhenFormatting());
        }
    }
}
