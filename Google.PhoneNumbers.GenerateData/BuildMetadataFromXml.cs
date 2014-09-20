using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace Google.PhoneNumbers.GenerateData
{
    public class BuildMetadataFromXml
    {
//  private const Logger LOGGER = Logger.getLogger(BuildMetadataFromXml.class.getName());

  // String constants used to fetch the XML nodes and attributes.
  private const String CARRIER_CODE_FORMATTING_RULE = "carrierCodeFormattingRule";
  private const String CARRIER_SPECIFIC = "carrierSpecific";
  private const String COUNTRY_CODE = "countryCode";
  private const String EMERGENCY = "emergency";
  private const String EXAMPLE_NUMBER = "exampleNumber";
  private const String FIXED_LINE = "fixedLine";
  private const String FORMAT = "format";
  private const String GENERAL_DESC = "generalDesc";
  private const String INTERNATIONAL_PREFIX = "internationalPrefix";
  private const String INTL_FORMAT = "intlFormat";
  private const String LEADING_DIGITS = "leadingDigits";
  private const String LEADING_ZERO_POSSIBLE = "leadingZeroPossible";
  private const String MAIN_COUNTRY_FOR_CODE = "mainCountryForCode";
  private const String MOBILE = "mobile";
  private const String MOBILE_NUMBER_PORTABLE_REGION = "mobileNumberPortableRegion";
  private const String NATIONAL_NUMBER_PATTERN = "nationalNumberPattern";
  private const String NATIONAL_PREFIX = "nationalPrefix";
  private const String NATIONAL_PREFIX_FORMATTING_RULE = "nationalPrefixFormattingRule";
  private const String NATIONAL_PREFIX_OPTIONAL_WHEN_FORMATTING =
      "nationalPrefixOptionalWhenFormatting";
  private const String NATIONAL_PREFIX_FOR_PARSING = "nationalPrefixForParsing";
  private const String NATIONAL_PREFIX_TRANSFORM_RULE = "nationalPrefixTransformRule";
  private const String NO_INTERNATIONAL_DIALLING = "noInternationalDialling";
  private const String NUMBER_FORMAT = "numberFormat";
  private const String PAGER = "pager";
  private const String PATTERN = "pattern";
  private const String PERSONAL_NUMBER = "personalNumber";
  private const String POSSIBLE_NUMBER_PATTERN = "possibleNumberPattern";
  private const String PREFERRED_EXTN_PREFIX = "preferredExtnPrefix";
  private const String PREFERRED_INTERNATIONAL_PREFIX = "preferredInternationalPrefix";
  private const String PREMIUM_RATE = "premiumRate";
  private const String SHARED_COST = "sharedCost";
  private const String SHORT_CODE = "shortCode";
  private const String STANDARD_RATE = "standardRate";
  private const String TOLL_FREE = "tollFree";
  private const String UAN = "uan";
  private const String VOICEMAIL = "voicemail";
  private const String VOIP = "voip";

  // Build the PhoneMetadataCollection from the input XML file.
  public static PhoneMetadataCollection buildPhoneMetadataCollection(String inputXmlFile, bool liteBuild) {
//    DocumentBuilderFactory builderFactory = DocumentBuilderFactory.newInstance();
//    DocumentBuilder builder = builderFactory.newDocumentBuilder();
      var document = new XmlDocument();
//    File xmlFile = new File(inputXmlFile);
//    Document document = builder.parse(xmlFile);
      document.Load(inputXmlFile);
      document.Normalize();
    var territory = document.GetElementsByTagName("territory");
    PhoneMetadataCollection.Builder metadataCollection = PhoneMetadataCollection.newBuilder();
      int numOfTerritories = territory.Count;
    // TODO: Look for other uses of these constants and possibly pull them out into
    // a separate constants file.
    bool isShortNumberMetadata = inputXmlFile.Contains("ShortNumberMetadata");
    bool isAlternateFormatsMetadata = inputXmlFile.Contains("PhoneNumberAlternateFormats");
    for (int i = 0; i < numOfTerritories; i++)
    {
        XmlElement territoryXmlElement = (XmlElement) territory.Item(i);
      String regionCode = "";
      // For the main metadata file this should always be set, but for other supplementary data
      // files the country calling code may be all that is needed.
      if (territoryXmlElement.HasAttribute("id")) {
        regionCode = territoryXmlElement.GetAttribute("id");
      }
      PhoneMetadata metadata = loadCountryMetadata(regionCode, territoryXmlElement, liteBuild,
          isShortNumberMetadata, isAlternateFormatsMetadata);
      metadataCollection.addMetadata(metadata);
    }
    return metadataCollection.build();
  }

  // Build a mapping from a country calling code to the region codes which denote the country/region
  // represented by that country code. In the case of multiple countries sharing a calling code,
  // such as the NANPA countries, the one indicated with "isMainCountryForCode" in the metadata
  // should be first.
  public static Dictionary<int, List<String>> buildCountryCodeToRegionCodeMap(
      PhoneMetadataCollection metadataCollection) {
    var countryCodeToRegionCodeMap = new Dictionary<int, List<String>>();
    foreach (PhoneMetadata metadata in metadataCollection.getMetadataList()) {
      String regionCode = metadata.getId();
      int countryCode = metadata.getCountryCode();
      if (countryCodeToRegionCodeMap.ContainsKey(countryCode)) {
        if (metadata.getMainCountryForCode()) {
          countryCodeToRegionCodeMap[countryCode].Insert(0, regionCode);
        } else {
          countryCodeToRegionCodeMap[countryCode].Add(regionCode);
        }
      } else {
        // For most countries, there will be only one region code for the country calling code.
        List<String> listWithRegionCode = new List<String>(1);
        if (regionCode.Length != 0) {  // For alternate formats, there are no region codes at all.
          listWithRegionCode.Add(regionCode);
        }
        countryCodeToRegionCodeMap.Add(countryCode, listWithRegionCode);
      }
    }
    return countryCodeToRegionCodeMap;
  }

  private static String validateRE(String regex) {
    return validateRE(regex, false);
  }

  // @VisibleForTesting
  internal static String validateRE(String regex, bool removeWhitespace) {
    // Removes all the whitespace and newline from the regexp. Not using pattern compile options to
    // make it work across programming languages.
    String compressedRegex = removeWhitespace ? Regex.Replace(regex, "\\s", "") : regex;
    new Regex(compressedRegex);
    // We don't ever expect to see | followed by a ) in our metadata - this would be an indication
    // of a bug. If one wants to make something optional, we prefer ? to using an empty group.
    int errorIndex = compressedRegex.IndexOf("|)", StringComparison.InvariantCulture);
    if (errorIndex >= 0) {
//      LOGGER.log(Level.SEVERE,
//                 "Error with original regex: " + regex + "\n| should not be followed directly " +
//                 "by ) in phone number regular expressions.");
        throw new ArgumentException("| followed by ) in regex" + compressedRegex + " at " + errorIndex);
    }
    // return the regex if it is of correct syntax, i.e. compile did not fail with a
    // PatternSyntaxException.
    return compressedRegex;
  }

  /**
   * Returns the national prefix of the provided country element.
   */
  // @VisibleForTesting
  internal static String getNationalPrefix(XmlElement element) {
    return element.HasAttribute(NATIONAL_PREFIX) ? element.GetAttribute(NATIONAL_PREFIX) : "";
  }

  // @VisibleForTesting
  internal static PhoneMetadata.Builder loadTerritoryTagMetadata(String regionCode, XmlElement element,
                                                        String nationalPrefix) {
    PhoneMetadata.Builder metadata = PhoneMetadata.newBuilder();
    metadata.setId(regionCode);
    if (element.HasAttribute(COUNTRY_CODE)) {
      metadata.setCountryCode(int.Parse(element.GetAttribute(COUNTRY_CODE)));
    }
    if (element.HasAttribute(LEADING_DIGITS)) {
      metadata.setLeadingDigits(validateRE(element.GetAttribute(LEADING_DIGITS)));
    }
    metadata.setInternationalPrefix(validateRE(element.GetAttribute(INTERNATIONAL_PREFIX)));
    if (element.HasAttribute(PREFERRED_INTERNATIONAL_PREFIX)) {
      String preferredInternationalPrefix = element.GetAttribute(PREFERRED_INTERNATIONAL_PREFIX);
      metadata.setPreferredInternationalPrefix(preferredInternationalPrefix);
    }
    if (element.HasAttribute(NATIONAL_PREFIX_FOR_PARSING)) {
      metadata.setNationalPrefixForParsing(
          validateRE(element.GetAttribute(NATIONAL_PREFIX_FOR_PARSING), true));
      if (element.HasAttribute(NATIONAL_PREFIX_TRANSFORM_RULE)) {
        metadata.setNationalPrefixTransformRule(
            validateRE(element.GetAttribute(NATIONAL_PREFIX_TRANSFORM_RULE)));
      }
    }
    if (nationalPrefix.Length != 0) {
      metadata.setNationalPrefix(nationalPrefix);
      if (!metadata.HasNationalPrefixForParsing()) {
        metadata.setNationalPrefixForParsing(nationalPrefix);
      }
    }
    if (element.HasAttribute(PREFERRED_EXTN_PREFIX)) {
      metadata.setPreferredExtnPrefix(element.GetAttribute(PREFERRED_EXTN_PREFIX));
    }
    if (element.HasAttribute(MAIN_COUNTRY_FOR_CODE)) {
      metadata.setMainCountryForCode(true);
    }
    if (element.HasAttribute(LEADING_ZERO_POSSIBLE)) {
      metadata.setLeadingZeroPossible(true);
    }
    if (element.HasAttribute(MOBILE_NUMBER_PORTABLE_REGION)) {
      metadata.setMobileNumberPortableRegion(true);
    }
    return metadata;
  }

  /**
   * Extracts the pattern for international format. If there is no intlFormat, default to using the
   * national format. If the intlFormat is set to "NA" the intlFormat should be ignored.
   *
   * @throws  RuntimeException if multiple intlFormats have been encountered.
   * @return  whether an international number format is defined.
   */
  // @VisibleForTesting
  internal static bool loadInternationalFormat(PhoneMetadata.Builder metadata,
                                         XmlElement numberFormatXmlElement,
                                         NumberFormat nationalFormat) {
    NumberFormat.Builder intlFormat = NumberFormat.newBuilder();
    var intlFormatPattern = numberFormatXmlElement.GetElementsByTagName(INTL_FORMAT);
    var hasExplicitIntlFormatDefined = false;

    if (intlFormatPattern.Count > 1) {
//      LOGGER.log(Level.SEVERE,
//                 "A maximum of one intlFormat pattern for a numberFormat element should be " +
//                 "defined.");
      String countryId = metadata.getId().Length > 0 ?
          metadata.getId() : metadata.getCountryCode().ToString();
      throw new Exception("Invalid number of intlFormat patterns for country: " + countryId);
    } else if (intlFormatPattern.Count == 0) {
      // Default to use the same as the national pattern if none is defined.
      intlFormat.mergeFrom(nationalFormat);
    } else {
      intlFormat.setPattern(numberFormatXmlElement.GetAttribute(PATTERN));
      setLeadingDigitsPatterns(numberFormatXmlElement, intlFormat);
      String intlFormatPatternValue = intlFormatPattern.Item(0).FirstChild.Value;
      if (!intlFormatPatternValue.Equals("NA")) {
        intlFormat.setFormat(intlFormatPatternValue);
      }
      hasExplicitIntlFormatDefined = true;
    }

    if (intlFormat.HasFormat()) {
      metadata.addIntlNumberFormat(intlFormat);
    }
    return hasExplicitIntlFormatDefined;
  }

  /**
   * Extracts the pattern for the national format.
   *
   * @throws  RuntimeException if multiple or no formats have been encountered.
   */
  // @VisibleForTesting
  internal static void loadNationalFormat(PhoneMetadata.Builder metadata, XmlElement numberFormatXmlElement,
                                 NumberFormat.Builder format) {
    setLeadingDigitsPatterns(numberFormatXmlElement, format);
    format.setPattern(validateRE(numberFormatXmlElement.GetAttribute(PATTERN)));

    var formatPattern = numberFormatXmlElement.GetElementsByTagName(FORMAT);
    int numFormatPatterns = formatPattern.Count;
    if (numFormatPatterns != 1) {
//      LOGGER.log(Level.SEVERE, "One format pattern for a numberFormat element should be defined.");
      String countryId = metadata.getId().Length > 0 ?
          metadata.getId() : metadata.getCountryCode().ToString();
      throw new Exception("Invalid number of format patterns (" + numFormatPatterns +
                                 ") for country: " + countryId);
    }
    format.setFormat(formatPattern.Item(0).FirstChild.Value);
  }

  /**
   *  Extracts the available formats from the provided DOM element. If it does not contain any
   *  nationalPrefixFormattingRule, the one passed-in is retained. The nationalPrefix,
   *  nationalPrefixFormattingRule and nationalPrefixOptionalWhenFormatting values are provided from
   *  the parent (territory) element.
   */
  // @VisibleForTesting
  internal static void loadAvailableFormats(PhoneMetadata.Builder metadata,
                                   XmlElement element, String nationalPrefix,
                                   String nationalPrefixFormattingRule,
                                   bool nationalPrefixOptionalWhenFormatting) {
    String carrierCodeFormattingRule = "";
    if (element.HasAttribute(CARRIER_CODE_FORMATTING_RULE)) {
      carrierCodeFormattingRule = validateRE(
          getDomesticCarrierCodeFormattingRuleFromXmlElement(element, nationalPrefix));
    }
    var numberFormatXmlElements = element.GetElementsByTagName(NUMBER_FORMAT);
    bool hasExplicitIntlFormatDefined = false;

      int numOfFormatXmlElements = numberFormatXmlElements.Count;
    if (numOfFormatXmlElements > 0) {
      for (int i = 0; i < numOfFormatXmlElements; i++) {
        XmlElement numberFormatXmlElement = (XmlElement) numberFormatXmlElements.Item(i);
        NumberFormat.Builder format = NumberFormat.newBuilder();

        if (numberFormatXmlElement.HasAttribute(NATIONAL_PREFIX_FORMATTING_RULE)) {
          format.setNationalPrefixFormattingRule(
              getNationalPrefixFormattingRuleFromXmlElement(numberFormatXmlElement, nationalPrefix));
        } else {
          format.setNationalPrefixFormattingRule(nationalPrefixFormattingRule);
        }

        if (format.HasNationalPrefixFormattingRule()) {
          if (numberFormatXmlElement.HasAttribute(NATIONAL_PREFIX_OPTIONAL_WHEN_FORMATTING)) {
            format.setNationalPrefixOptionalWhenFormatting(
                Boolean.Parse(numberFormatXmlElement.GetAttribute(
                    NATIONAL_PREFIX_OPTIONAL_WHEN_FORMATTING)));
          } else {
            format.setNationalPrefixOptionalWhenFormatting(nationalPrefixOptionalWhenFormatting);
          }
        }
        if (numberFormatXmlElement.HasAttribute(CARRIER_CODE_FORMATTING_RULE)) {
          format.setDomesticCarrierCodeFormattingRule(validateRE(
              getDomesticCarrierCodeFormattingRuleFromXmlElement(numberFormatXmlElement,
                                                              nationalPrefix)));
        } else {
          format.setDomesticCarrierCodeFormattingRule(carrierCodeFormattingRule);
        }
        loadNationalFormat(metadata, numberFormatXmlElement, format);
        metadata.addNumberFormat(format);

        if (loadInternationalFormat(metadata, numberFormatXmlElement, format.build())) {
          hasExplicitIntlFormatDefined = true;
        }
      }
      // Only a small number of regions need to specify the intlFormats in the xml. For the majority
      // of countries the intlNumberFormat metadata is an exact copy of the national NumberFormat
      // metadata. To minimize the size of the metadata file, we only keep intlNumberFormats that
      // actually differ in some way to the national formats.
      if (!hasExplicitIntlFormatDefined) {
        metadata.clearIntlNumberFormat();
      }
    }
  }

  // @VisibleForTesting
  internal static void setLeadingDigitsPatterns(XmlElement numberFormatXmlElement, NumberFormat.Builder format) {
    var leadingDigitsPatternNodes = numberFormatXmlElement.GetElementsByTagName(LEADING_DIGITS);
    int numOfLeadingDigitsPatterns = leadingDigitsPatternNodes.Count;
    if (numOfLeadingDigitsPatterns > 0) {
      for (int i = 0; i < numOfLeadingDigitsPatterns; i++) {
        format.addLeadingDigitsPattern(
            validateRE((leadingDigitsPatternNodes.Item(i)).FirstChild.Value, true));
      }
    }
  }

 

  // @VisibleForTesting
  internal static String getNationalPrefixFormattingRuleFromXmlElement(XmlElement element,
                                                           String nationalPrefix) {
    String nationalPrefixFormattingRule = element.GetAttribute(NATIONAL_PREFIX_FORMATTING_RULE);
    // Replace $NP with national prefix and $FG with the first group ($1).
      var npRegex = new Regex("\\$NP");
      var fgRegex = new Regex("\\$FG");
    nationalPrefixFormattingRule = npRegex.Replace(nationalPrefixFormattingRule, nationalPrefix, 1);
    nationalPrefixFormattingRule = fgRegex.Replace(nationalPrefixFormattingRule, "$1");
    return nationalPrefixFormattingRule;
  }

  // @VisibleForTesting
  internal static String getDomesticCarrierCodeFormattingRuleFromXmlElement(XmlElement element,
                                                                String nationalPrefix) {
    String carrierCodeFormattingRule = element.GetAttribute(CARRIER_CODE_FORMATTING_RULE);
    // Replace $FG with the first group ($1) and $NP with the national prefix.
    var npRegex = new Regex("\\$NP");
    var fgRegex = new Regex("\\$FG");
    carrierCodeFormattingRule = fgRegex.Replace(carrierCodeFormattingRule, "$1", 1);
    carrierCodeFormattingRule = npRegex.Replace(carrierCodeFormattingRule, nationalPrefix, 1);
    return carrierCodeFormattingRule;
  }

  // @VisibleForTesting
  internal static bool isValidNumberType(String numberType) {
    return numberType.Equals(FIXED_LINE) || numberType.Equals(MOBILE) ||
         numberType.Equals(GENERAL_DESC);
  }

  /**
   * Processes a phone number description element from the XML file and returns it as a
   * PhoneNumberDesc. If the description element is a fixed line or mobile number, the general
   * description will be used to fill in the whole element if necessary, or any components that are
   * missing. For all other types, the general description will only be used to fill in missing
   * components if the type has a partial definition. For example, if no "tollFree" element exists,
   * we assume there are no toll free numbers for that locale, and return a phone number description
   * with "NA" for both the national and possible number patterns.
   *
   * @param generalDesc  a generic phone number description that will be used to fill in missing
   *                     parts of the description
   * @param countryXmlElement  the XML element representing all the country information
   * @param numberType  the name of the number type, corresponding to the appropriate tag in the XML
   *                    file with information about that type
   * @return  complete description of that phone number type
   */
  // @VisibleForTesting
  internal static PhoneNumberDesc.Builder processPhoneNumberDescElement(PhoneNumberDesc.Builder generalDesc,
                                                               XmlElement countryXmlElement,
                                                               String numberType,
                                                               bool liteBuild) {
    var phoneNumberDescList = countryXmlElement.GetElementsByTagName(numberType);
    PhoneNumberDesc.Builder numberDesc = PhoneNumberDesc.newBuilder();
    if (phoneNumberDescList.Count == 0 && !isValidNumberType(numberType)) {
      numberDesc.setNationalNumberPattern("NA");
      numberDesc.setPossibleNumberPattern("NA");
      return numberDesc;
    }
    numberDesc.mergeFrom(generalDesc.build());
    if (phoneNumberDescList.Count > 0) {
      XmlElement element = (XmlElement) phoneNumberDescList.Item(0);
      var possiblePattern = element.GetElementsByTagName(POSSIBLE_NUMBER_PATTERN);
      if (possiblePattern.Count > 0) {
        numberDesc.setPossibleNumberPattern(
            validateRE(possiblePattern.Item(0).FirstChild.Value, true));
      }

      var validPattern = element.GetElementsByTagName(NATIONAL_NUMBER_PATTERN);
      if (validPattern.Count > 0) {
        numberDesc.setNationalNumberPattern(
            validateRE(validPattern.Item(0).FirstChild.Value, true));
      }

      if (!liteBuild) {
        var exampleNumber = element.GetElementsByTagName(EXAMPLE_NUMBER);
        if (exampleNumber.Count > 0) {
          numberDesc.setExampleNumber(exampleNumber.Item(0).FirstChild.Value);
        }
      }
    }
    return numberDesc;
  }

  // @VisibleForTesting
  internal static void setRelevantDescPatterns(PhoneMetadata.Builder metadata, XmlElement element,
      bool liteBuild, bool isShortNumberMetadata) {
    PhoneNumberDesc.Builder generalDesc = PhoneNumberDesc.newBuilder();
    generalDesc = processPhoneNumberDescElement(generalDesc, element, GENERAL_DESC, liteBuild);
    metadata.setGeneralDesc(generalDesc);

    if (!isShortNumberMetadata) {
      // Set fields used only by regular length phone numbers.
      metadata.setFixedLine(
          processPhoneNumberDescElement(generalDesc, element, FIXED_LINE, liteBuild));
      metadata.setMobile(
          processPhoneNumberDescElement(generalDesc, element, MOBILE, liteBuild));
      metadata.setSharedCost(
          processPhoneNumberDescElement(generalDesc, element, SHARED_COST, liteBuild));
      metadata.setVoip(
          processPhoneNumberDescElement(generalDesc, element, VOIP, liteBuild));
      metadata.setPersonalNumber(
          processPhoneNumberDescElement(generalDesc, element, PERSONAL_NUMBER, liteBuild));
      metadata.setPager(
          processPhoneNumberDescElement(generalDesc, element, PAGER, liteBuild));
      metadata.setUan(
          processPhoneNumberDescElement(generalDesc, element, UAN, liteBuild));
      metadata.setVoicemail(
          processPhoneNumberDescElement(generalDesc, element, VOICEMAIL, liteBuild));
      metadata.setNoInternationalDialling(
          processPhoneNumberDescElement(generalDesc, element, NO_INTERNATIONAL_DIALLING,
          liteBuild));
      metadata.setSameMobileAndFixedLinePattern(
          metadata.getMobile().getNationalNumberPattern().Equals(
          metadata.getFixedLine().getNationalNumberPattern()));
    } else {
      // Set fields used only by short numbers.
      metadata.setStandardRate(
          processPhoneNumberDescElement(generalDesc, element, STANDARD_RATE, liteBuild));
      metadata.setShortCode(
          processPhoneNumberDescElement(generalDesc, element, SHORT_CODE, liteBuild));
      metadata.setCarrierSpecific(
          processPhoneNumberDescElement(generalDesc, element, CARRIER_SPECIFIC, liteBuild));
      metadata.setEmergency(
          processPhoneNumberDescElement(generalDesc, element, EMERGENCY, liteBuild));
    }

    // Set fields used by both regular length and short numbers.
    metadata.setTollFree(
        processPhoneNumberDescElement(generalDesc, element, TOLL_FREE, liteBuild));
    metadata.setPremiumRate(
        processPhoneNumberDescElement(generalDesc, element, PREMIUM_RATE, liteBuild));
  }

  // @VisibleForTesting
  internal static PhoneMetadata loadCountryMetadata(String regionCode, XmlElement element, bool liteBuild,
      bool isShortNumberMetadata, bool isAlternateFormatsMetadata) {
    String nationalPrefix = getNationalPrefix(element);
    PhoneMetadata.Builder metadata =
        loadTerritoryTagMetadata(regionCode, element, nationalPrefix);
    String nationalPrefixFormattingRule =
        getNationalPrefixFormattingRuleFromXmlElement(element, nationalPrefix);
    loadAvailableFormats(metadata, element, nationalPrefix.ToString(),
                         nationalPrefixFormattingRule.ToString(),
                         element.HasAttribute(NATIONAL_PREFIX_OPTIONAL_WHEN_FORMATTING));
    if (!isAlternateFormatsMetadata) {
      // The alternate formats metadata does not need most of the patterns to be set.
      setRelevantDescPatterns(metadata, element, liteBuild, isShortNumberMetadata);
    }
    return metadata.build();
  }
}
}
