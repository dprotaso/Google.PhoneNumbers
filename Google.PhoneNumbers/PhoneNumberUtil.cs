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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Google.PhoneNumbers
{
   /**
 * Utility for international phone numbers. Functionality includes formatting, parsing and
 * validation.
 *
 * <p>If you use this library, and want to be notified about important changes, please sign up to
 * our <a href="http://groups.google.com/group/libphonenumber-discuss/about">mailing list</a>.
 *
 * NOTE: A lot of methods in this class require Region Code strings. These must be provided using
 * ISO 3166-1 two-letter country-code format. These should be in upper-case. The list of the codes
 * can be found here:
 * http://www.iso.org/iso/country_codes/iso_3166_code_lists/country_names_and_code_elements.htm
 *
 * @author Shaopeng Jia
 */
public class PhoneNumberUtil {

    private static readonly Logger logger = Logger.getLogger(typeof(PhoneNumberUtil));

    private class DefaultLoader : MetadataLoader
    {
        public Stream loadMetadata(string metadataFileName)
        {
            var assembly = typeof(PhoneNumberUtil).Assembly;
            return assembly.GetManifestResourceStream(metadataFileName);
        }
    }
  // @VisibleForTesting
  internal static readonly MetadataLoader DEFAULT_METADATA_LOADER =  new DefaultLoader();
  /** Flags to use when compiling regular expressions for phone numbers. */
  internal const RegexOptions REGEX_FLAGS = RegexOptions.IgnoreCase; // TODO: Make sure this supports unicode case folding
  // The minimum and maximum length of the national significant number.
  private const int MIN_LENGTH_FOR_NSN = 2;
  // The ITU says the maximum length should be 15, but we have found longer numbers in Germany.
  internal const int MAX_LENGTH_FOR_NSN = 17;
  // The maximum length of the country calling code.
  internal const int MAX_LENGTH_COUNTRY_CODE = 3;
  // We don't allow input strings for parsing to be longer than 250 chars. This prevents malicious
  // input from overflowing the regular-expression engine.
  private const int MAX_INPUT_STRING_LENGTH = 250;

  private const String META_DATA_FILE_PREFIX =
      "Google.PhoneNumbers.Data.PhoneNumberMetadataProto";

  // Region-code for the unknown region.
  private const String UNKNOWN_REGION = "ZZ";

  private const int NANPA_COUNTRY_CODE = 1;

  // The prefix that needs to be inserted in front of a Colombian landline number when dialed from
  // a mobile phone in Colombia.
  private const String COLOMBIA_MOBILE_TO_FIXED_LINE_PREFIX = "3";

  // Map of country calling codes that use a mobile token before the area code. One example of when
  // this is relevant is when determining the length of the national destination code, which should
  // be the length of the area code plus the length of the mobile token.
  private static readonly Dictionary<int, String> MOBILE_TOKEN_MAPPINGS;

  // The PLUS_SIGN signifies the international prefix.
  internal const char PLUS_SIGN = '+';

  private const char STAR_SIGN = '*';

  private const String RFC3966_EXTN_PREFIX = ";ext=";
  private const String RFC3966_PREFIX = "tel:";
  private const String RFC3966_PHONE_CONTEXT = ";phone-context=";
  private const String RFC3966_ISDN_SUBADDRESS = ";isub=";

  // A map that contains characters that are essential when dialling. That means any of the
  // characters in this map must not be removed from a number when dialling, otherwise the call
  // will not reach the intended destination.
  private static readonly IDictionary<char, char> DIALLABLE_CHAR_MAPPINGS;

  // Only upper-case variants of alpha characters are stored.
  private static readonly IDictionary<char, char> ALPHA_MAPPINGS;

  // For performance reasons, amalgamate both into one map.
  private static readonly IDictionary<char, char> ALPHA_PHONE_MAPPINGS;

  // Separate map of all symbols that we wish to retain when formatting alpha numbers. This
  // includes digits, ASCII letters and number grouping symbols such as "-" and " ".
  private static readonly IDictionary<char, char> ALL_PLUS_NUMBER_GROUPING_SYMBOLS;

  static PhoneNumberUtil() {
    Dictionary<int, String> mobileTokenMap = new Dictionary<int, String>();
    mobileTokenMap.Add(52, "1");
    mobileTokenMap.Add(54, "9");
    MOBILE_TOKEN_MAPPINGS = mobileTokenMap;

    // Simple ASCII digits map used to populate ALPHA_PHONE_MAPPINGS and
    // ALL_PLUS_NUMBER_GROUPING_SYMBOLS.
    Dictionary<char, char> asciiDigitMappings = new Dictionary<char, char>();
    asciiDigitMappings.Add('0', '0');
    asciiDigitMappings.Add('1', '1');
    asciiDigitMappings.Add('2', '2');
    asciiDigitMappings.Add('3', '3');
    asciiDigitMappings.Add('4', '4');
    asciiDigitMappings.Add('5', '5');
    asciiDigitMappings.Add('6', '6');
    asciiDigitMappings.Add('7', '7');
    asciiDigitMappings.Add('8', '8');
    asciiDigitMappings.Add('9', '9');

    Dictionary<char, char> alphaMap = new Dictionary<char, char>(40);
    alphaMap.Add('A', '2');
    alphaMap.Add('B', '2');
    alphaMap.Add('C', '2');
    alphaMap.Add('D', '3');
    alphaMap.Add('E', '3');
    alphaMap.Add('F', '3');
    alphaMap.Add('G', '4');
    alphaMap.Add('H', '4');
    alphaMap.Add('I', '4');
    alphaMap.Add('J', '5');
    alphaMap.Add('K', '5');
    alphaMap.Add('L', '5');
    alphaMap.Add('M', '6');
    alphaMap.Add('N', '6');
    alphaMap.Add('O', '6');
    alphaMap.Add('P', '7');
    alphaMap.Add('Q', '7');
    alphaMap.Add('R', '7');
    alphaMap.Add('S', '7');
    alphaMap.Add('T', '8');
    alphaMap.Add('U', '8');
    alphaMap.Add('V', '8');
    alphaMap.Add('W', '9');
    alphaMap.Add('X', '9');
    alphaMap.Add('Y', '9');
    alphaMap.Add('Z', '9');
    ALPHA_MAPPINGS = alphaMap;

    Dictionary<char, char> combinedMap = new Dictionary<char, char>(100);
    foreach(var entry in ALPHA_MAPPINGS) { combinedMap.Add(entry.Key, entry.Value);}
    foreach(var entry in asciiDigitMappings) { combinedMap.Add(entry.Key, entry.Value);}
    ALPHA_PHONE_MAPPINGS = combinedMap;

    Dictionary<char, char> diallableCharMap = new Dictionary<char, char>();
    foreach(var entry in asciiDigitMappings) { diallableCharMap.Add(entry.Key, entry.Value);}
    diallableCharMap.Add(PLUS_SIGN, PLUS_SIGN);
    diallableCharMap.Add('*', '*');
    DIALLABLE_CHAR_MAPPINGS = diallableCharMap;

    Dictionary<char, char> allPlusNumberGroupings = new Dictionary<char, char>();
    // Put (lower letter -> upper letter) and (upper letter -> upper letter) mappings.
    foreach (char c in ALPHA_MAPPINGS.Keys) {
      allPlusNumberGroupings.Add(char.ToLower(c), c);
      allPlusNumberGroupings.Add(c, c);
    }
    
    foreach(var entry in asciiDigitMappings) { allPlusNumberGroupings.Add(entry.Key, entry.Value);}
    // Put grouping symbols.
    allPlusNumberGroupings.Add('-', '-');
    allPlusNumberGroupings.Add('\uFF0D', '-');
    allPlusNumberGroupings.Add('\u2010', '-');
    allPlusNumberGroupings.Add('\u2011', '-');
    allPlusNumberGroupings.Add('\u2012', '-');
    allPlusNumberGroupings.Add('\u2013', '-');
    allPlusNumberGroupings.Add('\u2014', '-');
    allPlusNumberGroupings.Add('\u2015', '-');
    allPlusNumberGroupings.Add('\u2212', '-');
    allPlusNumberGroupings.Add('/', '/');
    allPlusNumberGroupings.Add('\uFF0F', '/');
    allPlusNumberGroupings.Add(' ', ' ');
    allPlusNumberGroupings.Add('\u3000', ' ');
    allPlusNumberGroupings.Add('\u2060', ' ');
    allPlusNumberGroupings.Add('.', '.');
    allPlusNumberGroupings.Add('\uFF0E', '.');
    ALL_PLUS_NUMBER_GROUPING_SYMBOLS = allPlusNumberGroupings;

    // One-character symbols that can be used to indicate an extension.
    String singleExtnSymbolsForMatching = "x\uFF58#\uFF03~\uFF5E";
    // For parsing, we are slightly more lenient in our interpretation than for matching. Here we
    // allow a "comma" as a possible extension indicator. When matching, this is hardly ever used to
    // indicate this.
    String singleExtnSymbolsForParsing = "," + singleExtnSymbolsForMatching;

    EXTN_PATTERNS_FOR_PARSING = createExtnRegex(singleExtnSymbolsForParsing);
    EXTN_PATTERNS_FOR_MATCHING = createExtnRegex(singleExtnSymbolsForMatching);
    EXTN_PATTERN = new JavaRegex("(?:" + EXTN_PATTERNS_FOR_PARSING + ")$", REGEX_FLAGS);

    
    VALID_ALPHA = String.Join(String.Empty, ALPHA_MAPPINGS.Keys) +
                  String.Join(String.Empty, ALPHA_MAPPINGS.Keys).ToLower();

    VALID_PHONE_NUMBER = DIGITS + "{" + MIN_LENGTH_FOR_NSN + "}" + "|" +
    "[" + PLUS_CHARS + "]*(?:[" + VALID_PUNCTUATION + STAR_SIGN + "]*" + DIGITS + "){3,}[" +
    VALID_PUNCTUATION + STAR_SIGN + VALID_ALPHA + DIGITS + "]*";
    VALID_PHONE_NUMBER_PATTERN = new JavaRegex(VALID_PHONE_NUMBER + "(?:" + EXTN_PATTERNS_FOR_PARSING + ")?", REGEX_FLAGS);
  }

  // Regex that makes it easy to distinguish whether a region has a unique international dialing
  // prefix or not. If a region has a unique international prefix (e.g. 011 in USA), it will be
  // represented as a string that contains a sequence of ASCII digits. If there are multiple
  // available international prefixes in a region, they will be represented as a regex string that
  // always contains character(s) other than ASCII digits.
  // Note this regex also includes tilde, which signals waiting for the tone.
  private static readonly JavaRegex UNIQUE_INTERNATIONAL_PREFIX =
      new JavaRegex("[\\d]+(?:[~\u2053\u223C\uFF5E][\\d]+)?");

  // Regular expression of acceptable punctuation found in phone numbers. This excludes punctuation
  // found as a leading character only.
  // This consists of dash characters, white space characters, full stops, slashes,
  // square brackets, parentheses and tildes. It also includes the letter 'x' as that is found as a
  // placeholder for carrier information in some phone numbers. Full-width variants are also
  // present.
  internal const string VALID_PUNCTUATION = "-x\u2010-\u2015\u2212\u30FC\uFF0D-\uFF0F " +
      "\u00A0\u00AD\u200B\u2060\u3000()\uFF08\uFF09\uFF3B\uFF3D.\\[\\]/~\u2053\u223C\uFF5E";
  private const String DIGITS = "\\p{Nd}";
  // We accept alpha characters in phone numbers, ASCII only, upper and lower case.
    private static readonly String VALID_ALPHA; 
//      Arrays.ToString(ALPHA_MAPPINGS.keySet().toArray()).replaceAll("[, \\[\\]]", "") +
//      Arrays.ToString(ALPHA_MAPPINGS.keySet().toArray()).toLowerCase().replaceAll("[, \\[\\]]", "");
  internal const String PLUS_CHARS = "+\uFF0B";
  internal static readonly JavaRegex PLUS_CHARS_PATTERN = new JavaRegex("[" + PLUS_CHARS + "]+");
  private static readonly JavaRegex SEPARATOR_PATTERN = new JavaRegex("[" + VALID_PUNCTUATION + "]+");
  private static readonly JavaRegex CAPTURING_DIGIT_PATTERN = new JavaRegex("(" + DIGITS + ")");

  // Regular expression of acceptable characters that may start a phone number for the purposes of
  // parsing. This allows us to strip away meaningless prefixes to phone numbers that may be
  // mistakenly given to us. This consists of digits, the plus symbol and arabic-indic digits. This
  // does not contain alpha characters, although they may be used later in the number. It also does
  // not include other punctuation, as this will be stripped later during parsing and is of no
  // information value when parsing a number.
  private static readonly String VALID_START_CHAR = "[" + PLUS_CHARS + DIGITS + "]";
  private static readonly JavaRegex VALID_START_CHAR_PATTERN = new JavaRegex(VALID_START_CHAR);

  // Regular expression of characters typically used to start a second phone number for the purposes
  // of parsing. This allows us to strip off parts of the number that are actually the start of
  // another number, such as for: (530) 583-6985 x302/x2303 -> the second extension here makes this
  // actually two phone numbers, (530) 583-6985 x302 and (530) 583-6985 x2303. We remove the second
  // extension so that the first number is parsed correctly.
  private static readonly String SECOND_NUMBER_START = "[\\\\/] *x";
  internal static readonly JavaRegex SECOND_NUMBER_START_PATTERN = new JavaRegex(SECOND_NUMBER_START);

  // Regular expression of trailing characters that we want to remove. We remove all characters that
  // are not alpha or numerical characters. The hash character is retained here, as it may signify
  // the previous block was an extension.
  private static readonly String UNWANTED_END_CHARS = "[^\\p{N}\\p{L}#]+$";
  internal static readonly JavaRegex UNWANTED_END_CHAR_PATTERN = new JavaRegex(UNWANTED_END_CHARS);

  // We use this Regex to check if the phone number has at least three letters in it - if so, then
  // we treat it as a number where some phone-number digits are represented by letters.
  private static readonly JavaRegex VALID_ALPHA_PHONE_PATTERN = new JavaRegex("(?:.*?[A-Za-z]){3}.*");

  // Regular expression of viable phone numbers. This is location independent. Checks we have at
  // least three leading digits, and only valid punctuation, alpha characters and
  // digits in the phone number. Does not include extension data.
  // The symbol 'x' is allowed here as valid punctuation since it is often used as a placeholder for
  // carrier codes, for example in Brazilian phone numbers. We also allow multiple "+" characters at
  // the start.
  // Corresponds to the following:
  // [digits]{minLengthNsn}|
  // plus_sign*(([punctuation]|[star])*[digits]){3,}([punctuation]|[star]|[digits]|[alpha])*
  //
  // The first reg-ex is to allow short numbers (two digits long) to be parsed if they are entered
  // as "15" etc, but only if there is no punctuation in them. The second expression restricts the
  // number of digits to three or more, but then allows them to be in international form, and to
  // have alpha-characters and punctuation.
  //
  // Note VALID_PUNCTUATION starts with a -, so must be the first in the range.
    private static readonly String VALID_PHONE_NUMBER;
      

  // Default extension prefix to use when formatting. This will be put in front of any extension
  // component of the number, after the main national number is formatted. For example, if you wish
  // the default extension formatting to be " extn: 3456", then you should specify " extn: " here
  // as the default extension prefix. This can be overridden by region-specific preferences.
  private static readonly String DEFAULT_EXTN_PREFIX = " ext. ";

  // Regex to capture digits used in an extension. Places a maximum length of "7" for an
  // extension.
  private static readonly String CAPTURING_EXTN_DIGITS = "(" + DIGITS + "{1,7})";
  // Regexp of all possible ways to write extensions, for use when parsing. This will be run as a
  // case-insensitive regexp match. Wide character versions are also provided after each ASCII
  // version.
  private static readonly String EXTN_PATTERNS_FOR_PARSING;
  internal static readonly String EXTN_PATTERNS_FOR_MATCHING;

  /**
   * Helper initialiser method to create the regular-expression Regex to match extensions,
   * allowing the one-char extension symbols provided by {@code singleExtnSymbols}.
   */
  private static String createExtnRegex(String singleExtnSymbols) {
    // There are three regular expressions here. The first covers RFC 3966 format, where the
    // extension is added using ";ext=". The second more generic one starts with optional white
    // space and ends with an optional full stop (.), followed by zero or more spaces/tabs and then
    // the numbers themselves. The other one covers the special case of American numbers where the
    // extension is written with a hash at the end, such as "- 503#".
    // Note that the only capturing groups should be around the digits that you want to capture as
    // part of the extension, or else parsing will fail!
    // Canonical-equivalence doesn't seem to be an option with Android java, so we allow two options
    // for representing the accented o - the character itself, and one in the unicode decomposed
    // form with the combining acute accent.
    return (RFC3966_EXTN_PREFIX + CAPTURING_EXTN_DIGITS + "|" + "[ \u00A0\\t,]*" +
            "(?:e?xt(?:ensi(?:o\u0301?|\u00F3))?n?|\uFF45?\uFF58\uFF54\uFF4E?|" +
            "[" + singleExtnSymbols + "]|int|anexo|\uFF49\uFF4E\uFF54)" +
            "[:\\.\uFF0E]?[ \u00A0\\t,-]*" + CAPTURING_EXTN_DIGITS + "#?|" +
            "[- ]+(" + DIGITS + "{1,5})#");
  }

  // Regexp of all known extension prefixes used by different regions followed by 1 or more valid
  // digits, for use when parsing.
    private static readonly JavaRegex EXTN_PATTERN;

  // We append optionally the extension Regex to the end here, as a valid phone number may
  // have an extension prefix appended, followed by 1 or more digits.
    private static readonly JavaRegex VALID_PHONE_NUMBER_PATTERN;
      

  internal static readonly JavaRegex NON_DIGITS_PATTERN = new JavaRegex("\\D+");

  // The FIRST_GROUP_PATTERN was originally set to $1 but there are some countries for which the
  // first group is not used in the national Regex (e.g. Argentina) so the $1 group does not match
  // correctly.  Therefore, we use \d, so that the first group actually used in the Regex will be
  // matched.
  private static readonly JavaRegex FIRST_GROUP_PATTERN = new JavaRegex("(\\$\\d)");
  private static readonly JavaRegex NP_PATTERN = new JavaRegex("\\$NP");
  private static readonly JavaRegex FG_PATTERN = new JavaRegex("\\$FG");
  private static readonly JavaRegex CC_PATTERN = new JavaRegex("\\$CC");

  // A Regex that is used to determine if the national prefix formatting rule has the first group
  // only, i.e., does not start with the national prefix. Note that the Regex explicitly allows
  // for unbalanced parentheses.
  private static readonly JavaRegex FIRST_GROUP_ONLY_PREFIX_PATTERN = new JavaRegex("\\(?\\$1\\)?");

  private static PhoneNumberUtil instance = null;

  public static readonly String REGION_CODE_FOR_NON_GEO_ENTITY = "001";

  /**
   * INTERNATIONAL and NATIONAL formats are consistent with the definition in ITU-T Recommendation
   * E123. For example, the number of the Google Switzerland office will be written as
   * "+41 44 668 1800" in INTERNATIONAL format, and as "044 668 1800" in NATIONAL format.
   * E164 format is as per INTERNATIONAL format but with no formatting applied, e.g.
   * "+41446681800". RFC3966 is as per INTERNATIONAL format, but with all spaces and other
   * separating symbols replaced with a hyphen, and with any phone number extension appended with
   * ";ext=". It also will have a prefix of "tel:" added, e.g. "tel:+41-44-668-1800".
   *
   * Note: If you are considering storing the number in a neutral format, you are highly advised to
   * use the PhoneNumber class.
   */
  public enum PhoneNumberFormat {
    E164,
    INTERNATIONAL,
    NATIONAL,
    RFC3966
  }

  /**
   * Type of phone numbers.
   */
  public enum PhoneNumberType {
    FIXED_LINE,
    MOBILE,
    // In some regions (e.g. the USA), it is impossible to distinguish between fixed-line and
    // mobile numbers by looking at the phone number itself.
    FIXED_LINE_OR_MOBILE,
    // Freephone lines
    TOLL_FREE,
    PREMIUM_RATE,
    // The cost of this call is shared between the caller and the recipient, and is hence typically
    // less than PREMIUM_RATE calls. See // http://en.wikipedia.org/wiki/Shared_Cost_Service for
    // more information.
    SHARED_COST,
    // Voice over IP numbers. This includes TSoIP (Telephony Service over IP).
    VOIP,
    // A personal number is associated with a particular person, and may be routed to either a
    // MOBILE or FIXED_LINE number. Some more information can be found here:
    // http://en.wikipedia.org/wiki/Personal_Numbers
    PERSONAL_NUMBER,
    PAGER,
    // Used for "Universal Access Numbers" or "Company Numbers". They may be further routed to
    // specific offices, but allow one number to be used for a company.
    UAN,
    // Used for "Voice Mail Access Numbers".
    VOICEMAIL,
    // A phone number is of type UNKNOWN when it does not fit any of the known Regexs for a
    // specific region.
    UNKNOWN
  }

  /**
   * Types of phone number matches. See detailed description beside the isNumberMatch() method.
   */
  public enum MatchType {
    NOT_A_NUMBER,
    NO_MATCH,
    SHORT_NSN_MATCH,
    NSN_MATCH,
    EXACT_MATCH,
  }

  /**
   * Possible outcomes when testing if a PhoneNumber is possible.
   */
  public enum ValidationResult {
    IS_POSSIBLE,
    INVALID_COUNTRY_CODE,
    TOO_SHORT,
    TOO_LONG,
  }

//  /**
//   * Leniency when {@linkplain PhoneNumberUtil#findNumbers finding} potential phone numbers in text
//   * segments. The levels here are ordered in increasing strictness.
//   */

  public enum Leniency {
//    /**
//     * Phone numbers accepted are {@linkplain PhoneNumberUtil#isPossibleNumber(PhoneNumber)
//     * possible}, but not necessarily {@linkplain PhoneNumberUtil#isValidNumber(PhoneNumber) valid}.
//     */
    POSSIBLE,
//    /**
//     * Phone numbers accepted are {@linkplain PhoneNumberUtil#isPossibleNumber(PhoneNumber)
//     * possible} and {@linkplain PhoneNumberUtil#isValidNumber(PhoneNumber) valid}. Numbers written
//     * in national format must have their national-prefix present if it is usually written for a
//     * number of this type.
//     */
    VALID,
//    /**
//     * Phone numbers accepted are {@linkplain PhoneNumberUtil#isValidNumber(PhoneNumber) valid} and
//     * are grouped in a possible way for this locale. For example, a US number written as
//     * "65 02 53 00 00" and "650253 0000" are not accepted at this leniency level, whereas
//     * "650 253 0000", "650 2530000" or "6502530000" are.
//     * Numbers with more than one '/' symbol in the national significant number are also dropped at
//     * this level.
//     * <p>
//     * Warning: This level might result in lower coverage especially for regions outside of country
//     * code "+1". If you are not sure about which level to use, email the discussion group
//     * libphonenumber-discuss@googlegroups.com.
//     */
    STRICT_GROUPING,
//    /**
//     * Phone numbers accepted are {@linkplain PhoneNumberUtil#isValidNumber(PhoneNumber) valid} and
//     * are grouped in the same way that we would have formatted it, or as a single block. For
//     * example, a US number written as "650 2530000" is not accepted at this leniency level, whereas
//     * "650 253 0000" or "6502530000" are.
//     * Numbers with more than one '/' symbol are also dropped at this level.
//     * <p>
//     * Warning: This level might result in lower coverage especially for regions outside of country
//     * code "+1". If you are not sure about which level to use, email the discussion group
//     * libphonenumber-discuss@googlegroups.com.
//     */
    EXACT_GROUPING,
  }

  // A mapping from a country calling code to the region codes which denote the region represented
  // by that country calling code. In the case of multiple regions sharing a calling code, such as
  // the NANPA regions, the one indicated with "isMainCountryForCode" in the metadata should be
  // first.
  private readonly IDictionary<int, List<String>> countryCallingCodeToRegionCodeMap;

  public IDictionary<int, List<String>> GetCountryCallingCodeToRegionCodeMap
  {
      get { return countryCallingCodeToRegionCodeMap; }
  }

  // The set of regions that share country calling code 1.
  // There are roughly 26 regions.
  // We set the initial capacity of the HashSet to 35 to offer a load factor of roughly 0.75.
  private readonly HashSet<String> nanpaRegions = new HashSet<String>();

  // A mapping from a region code to the PhoneMetadata for that region.
  // Note: Synchronization, though only needed for the Android version of the library, is used in
  // all versions for consistency.
  private readonly IDictionary<String, PhoneMetadata> regionToMetadataMap =
      new SynchronizedDictionary<String, PhoneMetadata>();

  // A mapping from a country calling code for a non-geographical entity to the PhoneMetadata for
  // that country calling code. Examples of the country calling codes include 800 (International
  // Toll Free Service) and 808 (International Shared Cost Service).
  // Note: Synchronization, though only needed for the Android version of the library, is used in
  // all versions for consistency.
  private readonly IDictionary<int, PhoneMetadata> countryCodeToNonGeographicalMetadataMap =
      new SynchronizedDictionary<int, PhoneMetadata>();

  // A cache for frequently used region-specific regular expressions.
  // The initial capacity is set to 100 as this seems to be an optimal value for Android, based on
  // performance measurements.
  private readonly RegexCache regexCache = new RegexCache(100);

  // The set of regions the library supports.
  // There are roughly 240 of them and we set the initial capacity of the HashSet to 320 to offer a
  // load factor of roughly 0.75.
  private readonly HashSet<String> supportedRegions = new HashSet<String>();

  // The set of county calling codes that map to the non-geo entity region ("001"). This set
  // currently contains < 12 elements so the default capacity of 16 (load factor=0.75) is fine.
  private readonly HashSet<int> countryCodesForNonGeographicalRegion = new HashSet<int>();

  // The prefix of the metadata files from which region data is loaded.
  private readonly String currentFilePrefix;
  // The metadata loader used to inject alternative metadata sources.
  private readonly MetadataLoader metadataLoader;

  /**
   * This class implements a singleton, the constructor is only visible to facilitate testing.
   */
  // @VisibleForTesting
  internal PhoneNumberUtil(String filePrefix, MetadataLoader metadataLoader,
      IDictionary<int, List<String>> countryCallingCodeToRegionCodeMap) {
    this.currentFilePrefix = filePrefix;
    this.metadataLoader = metadataLoader;
    this.countryCallingCodeToRegionCodeMap = countryCallingCodeToRegionCodeMap;
    foreach (var entry in countryCallingCodeToRegionCodeMap) {
      List<String> regionCodes = entry.Value;
      // We can assume that if the county calling code maps to the non-geo entity region code then
      // that's the only region code it maps to.
      if (regionCodes.Count == 1 && REGION_CODE_FOR_NON_GEO_ENTITY.Equals(regionCodes[0])) {
        // This is the subset of all country codes that map to the non-geo entity region code.
        countryCodesForNonGeographicalRegion.Add(entry.Key);
      } else {
        // The supported regions set does not include the "001" non-geo entity region code.
        foreach(var code in regionCodes) { supportedRegions.Add(code); }
      }
    }
    // If the non-geo entity still got added to the set of supported regions it must be because
    // there are entries that list the non-geo entity alongside normal regions (which is wrong).
    // If we discover this, remove the non-geo entity from the set of supported regions and log.
    if (supportedRegions.Remove(REGION_CODE_FOR_NON_GEO_ENTITY)) {
      logger.log(Level.WARNING, "invalid metadata " +
          "(country calling code was mapped to the non-geo entity as well as specific region(s))");
    }

    foreach(var code in countryCallingCodeToRegionCodeMap[NANPA_COUNTRY_CODE]) { nanpaRegions.Add(code); }
  }

  // @VisibleForTesting
  internal void loadMetadataFromFile(String filePrefix, String regionCode, int countryCallingCode,
      MetadataLoader metadataLoader) {
    bool isNonGeoRegion = REGION_CODE_FOR_NON_GEO_ENTITY.Equals(regionCode);
    String fileName = filePrefix + "_" +
        (isNonGeoRegion ? countryCallingCode.ToString() : regionCode);
    Stream source = metadataLoader.loadMetadata(fileName);
    if (source == null) {
      logger.log(Level.SEVERE, "missing metadata: " + fileName);
      throw new InvalidDataException("missing metadata: " + fileName);
    }
    BinaryReader reader = null;
    try {
      reader = new BinaryReader(source);
      PhoneMetadataCollection metadataCollection = loadMetadataAndCloseInput(reader);
      List<PhoneMetadata> metadataList = metadataCollection.getMetadataList();
      if (metadataList.Count == 0) {
        logger.log(Level.SEVERE, "empty metadata: " + fileName);
        throw new InvalidDataException("empty metadata: " + fileName);
      }
      if (metadataList.Count > 1) {
        logger.log(Level.WARNING, "invalid metadata (too many entries): " + fileName);
      }
      PhoneMetadata metadata = metadataList[0];
      if (isNonGeoRegion) {
        countryCodeToNonGeographicalMetadataMap.Add(countryCallingCode, metadata);
      } else {
        regionToMetadataMap.Add(regionCode, metadata);
      }
    } catch (IOException e) {
      logger.log(Level.SEVERE, "cannot load/parse metadata: " + fileName, e);
      throw new Exception("cannot load/parse metadata: " + fileName, e);
    }
  }

  /**
   * Loads the metadata protocol buffer from the given stream and closes the stream afterwards. Any
   * exceptions that occur while reading the stream are propagated (though exceptions that occur
   * when the stream is closed will be ignored).
   *
   * @param source  the non-null stream from which metadata is to be read.
   * @return        the loaded metadata protocol buffer.
   */
  private static PhoneMetadataCollection loadMetadataAndCloseInput(BinaryReader source) {
    PhoneMetadataCollection metadataCollection = new PhoneMetadataCollection();
    try {
      metadataCollection.readExternal(source);
    } catch (IOException e) {
      logger.log(Level.WARNING, "error reading input (ignored)", e);
    } finally {
      try {
        source.Dispose();
      } catch (IOException e) {
        logger.log(Level.WARNING, "error closing input stream (ignored)", e);
      }
    }
    return metadataCollection;
  }

  /**
   * Attempts to extract a possible number from the string passed in. This currently strips all
   * leading characters that cannot be used to start a phone number. Characters that can be used to
   * start a phone number are defined in the VALID_START_CHAR_PATTERN. If none of these characters
   * are found in the number passed in, an empty string is returned. This function also attempts to
   * strip off any alternative extensions or endings if two or more are present, such as in the case
   * of: (530) 583-6985 x302/x2303. The second extension here makes this actually two phone numbers,
   * (530) 583-6985 x302 and (530) 583-6985 x2303. We remove the second extension so that the first
   * number is parsed correctly.
   *
   * @param number  the string that might contain a phone number
   * @return        the number, stripped of any non-phone-number prefix (such as "Tel:") or an empty
   *                string if no character used to start phone numbers (such as + or any digit) is
   *                found in the number
   */
  internal static String extractPossibleNumber(String number) {
    var m = VALID_START_CHAR_PATTERN.Match(number);
    if (m.Success) {
      number = number.Substring(m.Index);
      // Remove trailing non-alpha non-numerical characters.
      var trailingCharsMatcher = UNWANTED_END_CHAR_PATTERN.Match(number);
      if (trailingCharsMatcher.Success) {
        number = number.Substring(0, trailingCharsMatcher.Index);
        logger.log(Level.FINER, "Stripped trailing characters: " + number);
      }
      // Check for extra numbers at the end.
      var secondNumber = SECOND_NUMBER_START_PATTERN.Match(number);
      if (secondNumber.Success) {
        number = number.Substring(0, secondNumber.Index);
      }
      return number;
    } else {
      return "";
    }
  }

  /**
   * Checks to see if the string of characters could possibly be a phone number at all. At the
   * moment, checks to see that the string begins with at least 2 digits, ignoring any punctuation
   * commonly found in phone numbers.
   * This method does not require the number to be normalized in advance - but does assume that
   * leading non-number symbols have been removed, such as by the method extractPossibleNumber.
   *
   * @param number  string to be checked for viability as a phone number
   * @return        true if the number could be a phone number of some sort, otherwise false
   */
  // @VisibleForTesting
  internal static bool isViablePhoneNumber(String number) {
    if (number.Length < MIN_LENGTH_FOR_NSN) {
      return false;
    }
    var m = VALID_PHONE_NUMBER_PATTERN.MatchWhole(number);
    return m.Success;
  }

  /**
   * Normalizes a string of characters representing a phone number. This performs the following
   * conversions:
   *   Punctuation is stripped.
   *   For ALPHA/VANITY numbers:
   *   Letters are converted to their numeric representation on a telephone keypad. The keypad
   *       used here is the one defined in ITU Recommendation E.161. This is only done if there are
   *       3 or more letters in the number, to lessen the risk that such letters are typos.
   *   For other numbers:
   *   Wide-ascii digits are converted to normal ASCII (European) digits.
   *   Arabic-Indic numerals are converted to European numerals.
   *   Spurious alpha characters are stripped.
   *
   * @param number  a string of characters representing a phone number
   * @return        the normalized string version of the phone number
   */
  internal static String normalize(String number) {
    var m = VALID_ALPHA_PHONE_PATTERN.MatchWhole(number);
    if (m.Success) {
      return normalizeHelper(number, ALPHA_PHONE_MAPPINGS, true);
    } else {
      return normalizeDigitsOnly(number);
    }
  }

  /**
   * Normalizes a string of characters representing a phone number. This is a wrapper for
   * normalize(String number) but does in-place normalization of the StringBuilder provided.
   *
   * @param number  a StringBuilder of characters representing a phone number that will be
   *     normalized in place
   */
  static void normalize(StringBuilder number) {
    String normalizedNumber = normalize(number.ToString());
    number.Clear();
    number.Append(normalizedNumber);
  }

  /**
   * Normalizes a string of characters representing a phone number. This converts wide-ascii and
   * arabic-indic numerals to European numerals, and strips punctuation and alpha characters.
   *
   * @param number  a string of characters representing a phone number
   * @return        the normalized string version of the phone number
   */
  public static String normalizeDigitsOnly(String number) {
    return normalizeDigits(number, false /* strip non-digits */).ToString();
  }

  internal static StringBuilder normalizeDigits(String number, bool keepNonDigits) {
    StringBuilder normalizedDigits = new StringBuilder(number.Length);
    foreach (char c in number.ToCharArray()) {
      var digit = (int) char.GetNumericValue(c);
      if (digit != -1) {
        normalizedDigits.Append(digit);
      } else if (keepNonDigits) {
        normalizedDigits.Append(c);
      }
    }
    return normalizedDigits;
  }

  /**
   * Normalizes a string of characters representing a phone number. This strips all characters which
   * are not diallable on a mobile phone keypad (including all non-ASCII digits).
   *
   * @param number  a string of characters representing a phone number
   * @return        the normalized string version of the phone number
   */
  internal static String normalizeDiallableCharsOnly(String number) {
    return normalizeHelper(number, DIALLABLE_CHAR_MAPPINGS, true /* remove non matches */);
  }

  /**
   * Converts all alpha characters in a number to their respective digits on a keypad, but retains
   * existing formatting.
   */
  public static String convertAlphaCharactersInNumber(String number) {
    return normalizeHelper(number, ALPHA_PHONE_MAPPINGS, false);
  }

  /**
   * Gets the length of the geographical area code from the
   * PhoneNumber object passed in, so that clients could use it
   * to split a national significant number into geographical area code and subscriber number. It
   * works in such a way that the resultant subscriber number should be diallable, at least on some
   * devices. An example of how this could be used:
   *
   * <pre>
   * PhoneNumberUtil phoneUtil = PhoneNumberUtil.getInstance();
   * PhoneNumber number = phoneUtil.parse("16502530000", "US");
   * String nationalSignificantNumber = phoneUtil.getNationalSignificantNumber(number);
   * String areaCode;
   * String subscriberNumber;
   *
   * int areaCodeLength = phoneUtil.getLengthOfGeographicalAreaCode(number);
   * if (areaCodeLength > 0) {
   *   areaCode = nationalSignificantNumber.Substring(0, areaCodeLength);
   *   subscriberNumber = nationalSignificantNumber.Substring(areaCodeLength);
   * } else {
   *   areaCode = "";
   *   subscriberNumber = nationalSignificantNumber;
   * }
   * </pre>
   *
   * N.B.: area code is a very ambiguous concept, so the I18N team generally recommends against
   * using it for most purposes, but recommends using the more general {@code national_number}
   * instead. Read the following carefully before deciding to use this method:
   * <ul>
   *  <li> geographical area codes change over time, and this method honors those changes;
   *    therefore, it doesn't guarantee the stability of the result it produces.
   *  <li> subscriber numbers may not be diallable from all devices (notably mobile devices, which
   *    typically requires the full national_number to be dialled in most regions).
   *  <li> most non-geographical numbers have no area codes, including numbers from non-geographical
   *    entities
   *  <li> some geographical numbers have no area codes.
   * </ul>
   * @param number  the PhoneNumber object for which clients
   *     want to know the length of the area code.
   * @return  the length of area code of the PhoneNumber object
   *     passed in.
   */
  public int getLengthOfGeographicalAreaCode(PhoneNumber number) {
    PhoneMetadata metadata = getMetadataForRegion(getRegionCodeForNumber(number));
    if (metadata == null) {
      return 0;
    }
    // If a country doesn't use a national prefix, and this number doesn't have an Italian leading
    // zero, we assume it is a closed dialling plan with no area codes.
    if (!metadata.HasNationalPrefix() && !number.isItalianLeadingZero()) {
      return 0;
    }

    if (!isNumberGeographical(number)) {
      return 0;
    }

    return getLengthOfNationalDestinationCode(number);
  }

  /**
   * Gets the length of the national destination code (NDC) from the
   * PhoneNumber object passed in, so that clients could use it
   * to split a national significant number into NDC and subscriber number. The NDC of a phone
   * number is normally the first group of digit(s) right after the country calling code when the
   * number is formatted in the international format, if there is a subscriber number part that
   * follows. An example of how this could be used:
   *
   * <pre>
   * PhoneNumberUtil phoneUtil = PhoneNumberUtil.getInstance();
   * PhoneNumber number = phoneUtil.parse("18002530000", "US");
   * String nationalSignificantNumber = phoneUtil.getNationalSignificantNumber(number);
   * String nationalDestinationCode;
   * String subscriberNumber;
   *
   * int nationalDestinationCodeLength = phoneUtil.getLengthOfNationalDestinationCode(number);
   * if (nationalDestinationCodeLength > 0) {
   *   nationalDestinationCode = nationalSignificantNumber.Substring(0,
   *       nationalDestinationCodeLength);
   *   subscriberNumber = nationalSignificantNumber.Substring(nationalDestinationCodeLength);
   * } else {
   *   nationalDestinationCode = "";
   *   subscriberNumber = nationalSignificantNumber;
   * }
   * </pre>
   *
   * Refer to the unittests to see the difference between this function and
   * {@link #getLengthOfGeographicalAreaCode}.
   *
   * @param number  the PhoneNumber object for which clients
   *     want to know the length of the NDC.
   * @return  the length of NDC of the PhoneNumber object
   *     passed in.
   */
  public int getLengthOfNationalDestinationCode(PhoneNumber number) {
    PhoneNumber copiedProto;
    if (number.HasExtension()) {
      // We don't want to alter the proto given to us, but we don't want to include the extension
      // when we format it, so we copy it and clear the extension here.
      copiedProto = new PhoneNumber();
      copiedProto.mergeFrom(number);
      copiedProto.clearExtension();
    } else {
      copiedProto = number;
    }

    String nationalSignificantNumber = format(copiedProto,
                                              PhoneNumberUtil.PhoneNumberFormat.INTERNATIONAL);
    String[] numberGroups = NON_DIGITS_PATTERN.Split(nationalSignificantNumber);
    // The Regex will start with "+COUNTRY_CODE " so the first group will always be the empty
    // string (before the + symbol) and the second group will be the country calling code. The third
    // group will be area code if it is not the last group.
    if (numberGroups.Length <= 3) {
      return 0;
    }

    if (getNumberType(number) == PhoneNumberType.MOBILE) {
      // For example Argentinian mobile numbers, when formatted in the international format, are in
      // the form of +54 9 NDC XXXX.... As a result, we take the length of the third group (NDC) and
      // add the length of the second group (which is the mobile token), which also forms part of
      // the national significant number. This assumes that the mobile token is always formatted
      // separately from the rest of the phone number.
      String mobileToken = getCountryMobileToken(number.getCountryCode());
      if (!mobileToken.Equals("")) {
        return numberGroups[2].Length + numberGroups[3].Length;
      }
    }
    return numberGroups[2].Length;
  }

  /**
   * Returns the mobile token for the provided country calling code if it has one, otherwise
   * returns an empty string. A mobile token is a number inserted before the area code when dialing
   * a mobile number from that country from abroad.
   *
   * @param countryCallingCode  the country calling code for which we want the mobile token
   * @return  the mobile token, as a string, for the given country calling code
   */
  public static String getCountryMobileToken(int countryCallingCode) {
    if (MOBILE_TOKEN_MAPPINGS.ContainsKey(countryCallingCode)) {
      return MOBILE_TOKEN_MAPPINGS[countryCallingCode];
    }
    return "";
  }

  /**
   * Normalizes a string of characters representing a phone number by replacing all characters found
   * in the accompanying map with the values therein, and stripping all other characters if
   * removeNonMatches is true.
   *
   * @param number                     a string of characters representing a phone number
   * @param normalizationReplacements  a mapping of characters to what they should be replaced by in
   *                                   the normalized version of the phone number
   * @param removeNonMatches           indicates whether characters that are not able to be replaced
   *                                   should be stripped from the number. If this is false, they
   *                                   will be left unchanged in the number.
   * @return  the normalized string version of the phone number
   */
  private static String normalizeHelper(String number,
                                        IDictionary<char, char> normalizationReplacements,
                                        bool removeNonMatches) {
    StringBuilder normalizedNumber = new StringBuilder(number.Length);
    for (int i = 0; i < number.Length; i++) {
      char character = number[i];
      char newDigit;
      if (normalizationReplacements.TryGetValue(char.ToUpper(character), out newDigit)) {
        normalizedNumber.Append(newDigit);
      } else if (!removeNonMatches) {
        normalizedNumber.Append(character);
      }
      // If neither of the above are true, we remove this character.
    }
    return normalizedNumber.ToString();
  }

  /**
   * Sets or resets the PhoneNumberUtil singleton instance. If set to null, the next call to
   * {@code getInstance()} will load (and return) the default instance.
   */
  // @VisibleForTesting
    // TODO Synchronize this method
  internal static void setInstance(PhoneNumberUtil util) {
    instance = util;
  }

  /**
   * Convenience method to get a list of what regions the library has metadata for.
   */
  public ISet<String> getSupportedRegions() {
    return supportedRegions;
  }

  /**
   * Convenience method to get a list of what global network calling codes the library has metadata
   * for.
   */
  public ISet<int> getSupportedGlobalNetworkCallingCodes() {
    return countryCodesForNonGeographicalRegion;
  }

  /**
   * Gets a {@link PhoneNumberUtil} instance to carry out international phone number formatting,
   * parsing, or validation. The instance is loaded with phone number metadata for a number of most
   * commonly used regions.
   *
   * <p>The {@link PhoneNumberUtil} is implemented as a singleton. Therefore, calling getInstance
   * multiple times will only result in one instance being created.
   *
   * @return a PhoneNumberUtil instance
   */
    //TODO Synchronize
  public static PhoneNumberUtil getInstance() {
    if (instance == null) {
      setInstance(createInstance(DEFAULT_METADATA_LOADER));
    }
    return instance;
  }

  /**
   * Create a new {@link PhoneNumberUtil} instance to carry out international phone number
   * formatting, parsing, or validation. The instance is loaded with all metadata by
   * using the metadataLoader specified.
   *
   * This method should only be used in the rare case in which you want to manage your own
   * metadata loading. Calling this method multiple times is very expensive, as each time
   * a new instance is created from scratch. When in doubt, use {@link #getInstance}.
   *
   * @param metadataLoader Customized metadata loader. If null, default metadata loader will
   *     be used. This should not be null.
   * @return a PhoneNumberUtil instance
   */
  public static PhoneNumberUtil createInstance(MetadataLoader metadataLoader) {
    if (metadataLoader == null) {
      throw new ArgumentException("metadataLoader could not be null.");
    }
    return new PhoneNumberUtil(META_DATA_FILE_PREFIX, metadataLoader,
        CountryCodeToRegionCodeMap.getCountryCodeToRegionCodeMap());
  }

  /**
   * Helper function to check if the national prefix formatting rule has the first group only, i.e.,
   * does not start with the national prefix.
   */
  internal static bool formattingRuleHasFirstGroupOnly(String nationalPrefixFormattingRule) {
    return nationalPrefixFormattingRule.Length == 0 ||
        FIRST_GROUP_ONLY_PREFIX_PATTERN.MatchWhole(nationalPrefixFormattingRule).Success;
  }

  /**
   * Tests whether a phone number has a geographical association. It checks if the number is
   * associated to a certain region in the country where it belongs to. Note that this doesn't
   * verify if the number is actually in use.
   *
   * A similar method is implemented as PhoneNumberOfflineGeocoder.canBeGeocoded, which performs a
   * looser check, since it only prevents cases where prefixes overlap for geocodable and
   * non-geocodable numbers. Also, if new phone number types were added, we should check if this
   * other method should be updated too.
   */
  internal bool isNumberGeographical(PhoneNumber phoneNumber) {
    PhoneNumberType numberType = getNumberType(phoneNumber);
    // TODO: Include mobile phone numbers from countries like Indonesia, which has some
    // mobile numbers that are geographical.
    return numberType == PhoneNumberType.FIXED_LINE ||
        numberType == PhoneNumberType.FIXED_LINE_OR_MOBILE;
  }

  /**
   * Helper function to check region code is not unknown or null.
   */
  private bool isValidRegionCode(String regionCode) {
    return regionCode != null && supportedRegions.Contains(regionCode);
  }

  /**
   * Helper function to check the country calling code is valid.
   */
  private bool hasValidCountryCallingCode(int countryCallingCode) {
    return countryCallingCodeToRegionCodeMap.ContainsKey(countryCallingCode);
  }

  /**
   * Formats a phone number in the specified format using default rules. Note that this does not
   * promise to produce a phone number that the user can dial from where they are - although we do
   * format in either 'national' or 'international' format depending on what the client asks for, we
   * do not currently support a more abbreviated format, such as for users in the same "area" who
   * could potentially dial the number without area code. Note that if the phone number has a
   * country calling code of 0 or an otherwise invalid country calling code, we cannot work out
   * which formatting rules to apply so we return the national significant number with no formatting
   * applied.
   *
   * @param number         the phone number to be formatted
   * @param numberFormat   the format the phone number should be formatted into
   * @return  the formatted phone number
   */
  public String format(PhoneNumber number, PhoneNumberFormat numberFormat) {
    if (number.getNationalNumber() == 0 && number.HasRawInput()) {
      // Unparseable numbers that kept their raw input just use that.
      // This is the only case where a number can be formatted as E164 without a
      // leading '+' symbol (but the original number wasn't parseable anyway).
      // TODO: Consider removing the 'if' above so that unparseable
      // strings without raw input format to the empty string instead of "+00"
      String rawInput = number.getRawInput();
      if (rawInput.Length > 0) {
        return rawInput;
      }
    }
    StringBuilder formattedNumber = new StringBuilder(20);
    format(number, numberFormat, formattedNumber);
    return formattedNumber.ToString();
  }

  /**
   * Same as {@link #format(PhoneNumber, PhoneNumberFormat)}, but accepts a mutable StringBuilder as
   * a parameter to decrease object creation when invoked many times.
   */
  public void format(PhoneNumber number, PhoneNumberFormat numberFormat,
                     StringBuilder formattedNumber) {
    // Clear the StringBuilder first.
    formattedNumber.Clear();
    int countryCallingCode = number.getCountryCode();
    String nationalSignificantNumber = getNationalSignificantNumber(number);

    if (numberFormat == PhoneNumberFormat.E164) {
      // Early exit for E164 case (even if the country calling code is invalid) since no formatting
      // of the national number needs to be applied. Extensions are not formatted.
      formattedNumber.Append(nationalSignificantNumber);
      prefixNumberWithCountryCallingCode(countryCallingCode, PhoneNumberFormat.E164,
                                         formattedNumber);
      return;
    }
    if (!hasValidCountryCallingCode(countryCallingCode)) {
      formattedNumber.Append(nationalSignificantNumber);
      return;
    }
    // Note getRegionCodeForCountryCode() is used because formatting information for regions which
    // share a country calling code is contained by only one region for performance reasons. For
    // example, for NANPA regions it will be contained in the metadata for US.
    String regionCode = getRegionCodeForCountryCode(countryCallingCode);
    // Metadata cannot be null because the country calling code is valid (which means that the
    // region code cannot be ZZ and must be one of our supported region codes).
    PhoneMetadata metadata =
        getMetadataForRegionOrCallingCode(countryCallingCode, regionCode);
    formattedNumber.Append(formatNsn(nationalSignificantNumber, metadata, numberFormat));
    maybeAppendFormattedExtension(number, metadata, numberFormat, formattedNumber);
    prefixNumberWithCountryCallingCode(countryCallingCode, numberFormat, formattedNumber);
  }

  /**
   * Formats a phone number in the specified format using client-defined formatting rules. Note that
   * if the phone number has a country calling code of zero or an otherwise invalid country calling
   * code, we cannot work out things like whether there should be a national prefix applied, or how
   * to format extensions, so we return the national significant number with no formatting applied.
   *
   * @param number                        the phone number to be formatted
   * @param numberFormat                  the format the phone number should be formatted into
   * @param userDefinedFormats            formatting rules specified by clients
   * @return  the formatted phone number
   */
  public String formatByPattern(PhoneNumber number,
                                PhoneNumberFormat numberFormat,
                                List<NumberFormat> userDefinedFormats) {
    int countryCallingCode = number.getCountryCode();
    String nationalSignificantNumber = getNationalSignificantNumber(number);
    if (!hasValidCountryCallingCode(countryCallingCode)) {
      return nationalSignificantNumber;
    }
    // Note getRegionCodeForCountryCode() is used because formatting information for regions which
    // share a country calling code is contained by only one region for performance reasons. For
    // example, for NANPA regions it will be contained in the metadata for US.
    String regionCode = getRegionCodeForCountryCode(countryCallingCode);
    // Metadata cannot be null because the country calling code is valid
    PhoneMetadata metadata =
        getMetadataForRegionOrCallingCode(countryCallingCode, regionCode);

    StringBuilder formattedNumber = new StringBuilder(20);

    NumberFormat formattingRegex =
        chooseFormattingRegexForNumber(userDefinedFormats, nationalSignificantNumber);
    if (formattingRegex == null) {
      // If no Regex above is matched, we format the number as a whole.
      formattedNumber.Append(nationalSignificantNumber);
    } else {
      NumberFormat numFormatCopy = new NumberFormat();
      // Before we do a replacement of the national prefix Regex $NP with the national prefix, we
      // need to copy the rule so that subsequent replacements for different numbers have the
      // appropriate national prefix.
      numFormatCopy.mergeFrom(formattingRegex);
      String nationalPrefixFormattingRule = formattingRegex.getNationalPrefixFormattingRule();
      if (nationalPrefixFormattingRule.Length > 0) {
        String nationalPrefix = metadata.getNationalPrefix();
        if (nationalPrefix.Length > 0) {
          // Replace $NP with national prefix and $FG with the first group ($1).
          nationalPrefixFormattingRule =
              NP_PATTERN.Replace(nationalPrefixFormattingRule, nationalPrefix, 1);
          nationalPrefixFormattingRule =
              FG_PATTERN.Replace(nationalPrefixFormattingRule, "$1", 1);
          numFormatCopy.setNationalPrefixFormattingRule(nationalPrefixFormattingRule);
        } else {
          // We don't want to have a rule for how to format the national prefix if there isn't one.
          numFormatCopy.clearNationalPrefixFormattingRule();
        }
      }
      formattedNumber.Append(
          formatNsnUsingRegex(nationalSignificantNumber, numFormatCopy, numberFormat));
    }
    maybeAppendFormattedExtension(number, metadata, numberFormat, formattedNumber);
    prefixNumberWithCountryCallingCode(countryCallingCode, numberFormat, formattedNumber);
    return formattedNumber.ToString();
  }

  /**
   * Formats a phone number in national format for dialing using the carrier as specified in the
   * {@code carrierCode}. The {@code carrierCode} will always be used regardless of whether the
   * phone number already has a preferred domestic carrier code stored. If {@code carrierCode}
   * contains an empty string, returns the number in national format without any carrier code.
   *
   * @param number  the phone number to be formatted
   * @param carrierCode  the carrier selection code to be used
   * @return  the formatted phone number in national format for dialing using the carrier as
   *          specified in the {@code carrierCode}
   */
  public String formatNationalNumberWithCarrierCode(PhoneNumber number, String carrierCode) {
    int countryCallingCode = number.getCountryCode();
    String nationalSignificantNumber = getNationalSignificantNumber(number);
    if (!hasValidCountryCallingCode(countryCallingCode)) {
      return nationalSignificantNumber;
    }

    // Note getRegionCodeForCountryCode() is used because formatting information for regions which
    // share a country calling code is contained by only one region for performance reasons. For
    // example, for NANPA regions it will be contained in the metadata for US.
    String regionCode = getRegionCodeForCountryCode(countryCallingCode);
    // Metadata cannot be null because the country calling code is valid.
    PhoneMetadata metadata = getMetadataForRegionOrCallingCode(countryCallingCode, regionCode);

    StringBuilder formattedNumber = new StringBuilder(20);
    formattedNumber.Append(formatNsn(nationalSignificantNumber, metadata,
                                     PhoneNumberFormat.NATIONAL, carrierCode));
    maybeAppendFormattedExtension(number, metadata, PhoneNumberFormat.NATIONAL, formattedNumber);
    prefixNumberWithCountryCallingCode(countryCallingCode, PhoneNumberFormat.NATIONAL,
                                       formattedNumber);
    return formattedNumber.ToString();
  }

  private PhoneMetadata getMetadataForRegionOrCallingCode(
      int countryCallingCode, String regionCode) {
    return REGION_CODE_FOR_NON_GEO_ENTITY.Equals(regionCode)
        ? getMetadataForNonGeographicalRegion(countryCallingCode)
        : getMetadataForRegion(regionCode);
  }

  /**
   * Formats a phone number in national format for dialing using the carrier as specified in the
   * preferredDomesticCarrierCode field of the PhoneNumber object passed in. If that is missing,
   * use the {@code fallbackCarrierCode} passed in instead. If there is no
   * {@code preferredDomesticCarrierCode}, and the {@code fallbackCarrierCode} contains an empty
   * string, return the number in national format without any carrier code.
   *
   * <p>Use {@link #formatNationalNumberWithCarrierCode} instead if the carrier code passed in
   * should take precedence over the number's {@code preferredDomesticCarrierCode} when formatting.
   *
   * @param number  the phone number to be formatted
   * @param fallbackCarrierCode  the carrier selection code to be used, if none is found in the
   *     phone number itself
   * @return  the formatted phone number in national format for dialing using the number's
   *     {@code preferredDomesticCarrierCode}, or the {@code fallbackCarrierCode} passed in if
   *     none is found
   */
  public String formatNationalNumberWithPreferredCarrierCode(PhoneNumber number,
                                                             String fallbackCarrierCode) {
    return formatNationalNumberWithCarrierCode(number, number.HasPreferredDomesticCarrierCode()
                                                       ? number.getPreferredDomesticCarrierCode()
                                                       : fallbackCarrierCode);
  }

  /**
   * Returns a number formatted in such a way that it can be dialed from a mobile phone in a
   * specific region. If the number cannot be reached from the region (e.g. some countries block
   * toll-free numbers from being called outside of the country), the method returns an empty
   * string.
   *
   * @param number  the phone number to be formatted
   * @param regionCallingFrom  the region where the call is being placed
   * @param withFormatting  whether the number should be returned with formatting symbols, such as
   *     spaces and dashes.
   * @return  the formatted phone number
   */
  public String formatNumberForMobileDialing(PhoneNumber number, String regionCallingFrom,
                                             bool withFormatting) {
    int countryCallingCode = number.getCountryCode();
    if (!hasValidCountryCallingCode(countryCallingCode)) {
      return number.HasRawInput() ? number.getRawInput() : "";
    }

    String formattedNumber = "";
    // Clear the extension, as that part cannot normally be dialed together with the main number.
    PhoneNumber numberNoExt = new PhoneNumber().mergeFrom(number).clearExtension();
    String regionCode = getRegionCodeForCountryCode(countryCallingCode);
    PhoneNumberType numberType = getNumberType(numberNoExt);
    bool isValidNumber = (numberType != PhoneNumberType.UNKNOWN);
    if (regionCallingFrom.Equals(regionCode)) {
      bool isFixedLineOrMobile =
          (numberType == PhoneNumberType.FIXED_LINE) || (numberType == PhoneNumberType.MOBILE) ||
          (numberType == PhoneNumberType.FIXED_LINE_OR_MOBILE);
      // Carrier codes may be needed in some countries. We handle this here.
      if (regionCode.Equals("CO") && numberType == PhoneNumberType.FIXED_LINE) {
        formattedNumber =
            formatNationalNumberWithCarrierCode(numberNoExt, COLOMBIA_MOBILE_TO_FIXED_LINE_PREFIX);
      } else if (regionCode.Equals("BR") && isFixedLineOrMobile) {
        formattedNumber = numberNoExt.HasPreferredDomesticCarrierCode()
            ? formattedNumber = formatNationalNumberWithPreferredCarrierCode(numberNoExt, "")
            // Brazilian fixed line and mobile numbers need to be dialed with a carrier code when
            // called within Brazil. Without that, most of the carriers won't connect the call.
            // Because of that, we return an empty string here.
            : "";
      } else if (isValidNumber && regionCode.Equals("HU")) {
        // The national format for HU numbers doesn't contain the national prefix, because that is
        // how numbers are normally written down. However, the national prefix is obligatory when
        // dialing from a mobile phone, except for short numbers. As a result, we add it back here
        // if it is a valid regular length phone number.
        formattedNumber =
            getNddPrefixForRegion(regionCode, true /* strip non-digits */) +
            " " + format(numberNoExt, PhoneNumberFormat.NATIONAL);
      } else if (countryCallingCode == NANPA_COUNTRY_CODE) {
        // For NANPA countries, we output international format for numbers that can be dialed
        // internationally, since that always works, except for numbers which might potentially be
        // short numbers, which are always dialled in national format.
        PhoneMetadata regionMetadata = getMetadataForRegion(regionCallingFrom);
        if (canBeInternationallyDialled(numberNoExt) &&
            !isShorterThanPossibleNormalNumber(regionMetadata,
                getNationalSignificantNumber(numberNoExt))) {
          formattedNumber = format(numberNoExt, PhoneNumberFormat.INTERNATIONAL);
        } else {
          formattedNumber = format(numberNoExt, PhoneNumberFormat.NATIONAL);
        }
      } else {
        // For non-geographical countries, and Mexican and Chilean fixed line and mobile numbers, we
        // output international format for numbers that can be dialed internationally as that always
        // works.
        if ((regionCode.Equals(REGION_CODE_FOR_NON_GEO_ENTITY) ||
            // MX fixed line and mobile numbers should always be formatted in international format,
            // even when dialed within MX. For national format to work, a carrier code needs to be
            // used, and the correct carrier code depends on if the caller and callee are from the
            // same local area. It is trickier to get that to work correctly than using
            // international format, which is tested to work fine on all carriers.
            // CL fixed line numbers need the national prefix when dialing in the national format,
            // but don't have it when used for display. The reverse is true for mobile numbers.
            // As a result, we output them in the international format to make it work.
            ((regionCode.Equals("MX") || regionCode.Equals("CL")) &&
             isFixedLineOrMobile)) &&
            canBeInternationallyDialled(numberNoExt)) {
          formattedNumber = format(numberNoExt, PhoneNumberFormat.INTERNATIONAL);
        } else {
          formattedNumber = format(numberNoExt, PhoneNumberFormat.NATIONAL);
        }
      }
    } else if (isValidNumber && canBeInternationallyDialled(numberNoExt)) {
      // We assume that short numbers are not diallable from outside their region, so if a number
      // is not a valid regular length phone number, we treat it as if it cannot be internationally
      // dialled.
      return withFormatting ? format(numberNoExt, PhoneNumberFormat.INTERNATIONAL)
                            : format(numberNoExt, PhoneNumberFormat.E164);
    }
    return withFormatting ? formattedNumber
                          : normalizeDiallableCharsOnly(formattedNumber);
  }

  /**
   * Formats a phone number for out-of-country dialing purposes. If no regionCallingFrom is
   * supplied, we format the number in its INTERNATIONAL format. If the country calling code is the
   * same as that of the region where the number is from, then NATIONAL formatting will be applied.
   *
   * <p>If the number itself has a country calling code of zero or an otherwise invalid country
   * calling code, then we return the number with no formatting applied.
   *
   * <p>Note this function takes care of the case for calling inside of NANPA and between Russia and
   * Kazakhstan (who share the same country calling code). In those cases, no international prefix
   * is used. For regions which have multiple international prefixes, the number in its
   * INTERNATIONAL format will be returned instead.
   *
   * @param number               the phone number to be formatted
   * @param regionCallingFrom    the region where the call is being placed
   * @return  the formatted phone number
   */
  public String formatOutOfCountryCallingNumber(PhoneNumber number,
                                                String regionCallingFrom) {
    if (!isValidRegionCode(regionCallingFrom)) {
      logger.log(Level.WARNING,
                 "Trying to format number from invalid region "
                 + regionCallingFrom
                 + ". International formatting applied.");
      return format(number, PhoneNumberFormat.INTERNATIONAL);
    }
    int countryCallingCode = number.getCountryCode();
    String nationalSignificantNumber = getNationalSignificantNumber(number);
    if (!hasValidCountryCallingCode(countryCallingCode)) {
      return nationalSignificantNumber;
    }
    if (countryCallingCode == NANPA_COUNTRY_CODE) {
      if (isNANPACountry(regionCallingFrom)) {
        // For NANPA regions, return the national format for these regions but prefix it with the
        // country calling code.
        return countryCallingCode + " " + format(number, PhoneNumberFormat.NATIONAL);
      }
    } else if (countryCallingCode == getCountryCodeForValidRegion(regionCallingFrom)) {
      // If regions share a country calling code, the country calling code need not be dialled.
      // This also applies when dialling within a region, so this if clause covers both these cases.
      // Technically this is the case for dialling from La Reunion to other overseas departments of
      // France (French Guiana, Martinique, Guadeloupe), but not vice versa - so we don't cover this
      // edge case for now and for those cases return the version including country calling code.
      // Details here: http://www.petitfute.com/voyage/225-info-pratiques-reunion
      return format(number, PhoneNumberFormat.NATIONAL);
    }
    // Metadata cannot be null because we checked 'isValidRegionCode()' above.
    PhoneMetadata metadataForRegionCallingFrom = getMetadataForRegion(regionCallingFrom);
    String internationalPrefix = metadataForRegionCallingFrom.getInternationalPrefix();

    // For regions that have multiple international prefixes, the international format of the
    // number is returned, unless there is a preferred international prefix.
    String internationalPrefixForFormatting = "";
    if (UNIQUE_INTERNATIONAL_PREFIX.MatchWhole(internationalPrefix).Success) {
      internationalPrefixForFormatting = internationalPrefix;
    } else if (metadataForRegionCallingFrom.HasPreferredInternationalPrefix()) {
      internationalPrefixForFormatting =
          metadataForRegionCallingFrom.getPreferredInternationalPrefix();
    }

    String regionCode = getRegionCodeForCountryCode(countryCallingCode);
    // Metadata cannot be null because the country calling code is valid.
    PhoneMetadata metadataForRegion =
        getMetadataForRegionOrCallingCode(countryCallingCode, regionCode);
    String formattedNationalNumber =
        formatNsn(nationalSignificantNumber, metadataForRegion, PhoneNumberFormat.INTERNATIONAL);
    StringBuilder formattedNumber = new StringBuilder(formattedNationalNumber);
    maybeAppendFormattedExtension(number, metadataForRegion, PhoneNumberFormat.INTERNATIONAL,
                                  formattedNumber);
    if (internationalPrefixForFormatting.Length > 0) {
      formattedNumber.Insert(0, " ").Insert(0, countryCallingCode).Insert(0, " ")
          .Insert(0, internationalPrefixForFormatting);
    } else {
      prefixNumberWithCountryCallingCode(countryCallingCode,
                                         PhoneNumberFormat.INTERNATIONAL,
                                         formattedNumber);
    }
    return formattedNumber.ToString();
  }

  /**
   * Formats a phone number using the original phone number format that the number is parsed from.
   * The original format is embedded in the country_code_source field of the PhoneNumber object
   * passed in. If such information is missing, the number will be formatted into the NATIONAL
   * format by default. When the number contains a leading zero and this is unexpected for this
   * country, or we don't have a formatting Regex for the number, the method returns the raw input
   * when it is available.
   *
   * Note this method guarantees no digit will be inserted, removed or modified as a result of
   * formatting.
   *
   * @param number  the phone number that needs to be formatted in its original number format
   * @param regionCallingFrom  the region whose IDD needs to be prefixed if the original number
   *     has one
   * @return  the formatted phone number in its original number format
   */
  public String formatInOriginalFormat(PhoneNumber number, String regionCallingFrom) {
    if (number.HasRawInput() &&
        (hasUnexpectedItalianLeadingZero(number) || !hasFormattingRegexForNumber(number))) {
      // We check if we have the formatting Regex because without that, we might format the number
      // as a group without national prefix.
      return number.getRawInput();
    }
    if (!number.HasCountryCodeSource()) {
      return format(number, PhoneNumberFormat.NATIONAL);
    }
    String formattedNumber;
    switch (number.getCountryCodeSource()) {
      case PhoneNumber.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN:
        formattedNumber = format(number, PhoneNumberFormat.INTERNATIONAL);
        break;
      case PhoneNumber.CountryCodeSource.FROM_NUMBER_WITH_IDD:
        formattedNumber = formatOutOfCountryCallingNumber(number, regionCallingFrom);
        break;
      case PhoneNumber.CountryCodeSource.FROM_NUMBER_WITHOUT_PLUS_SIGN:
        formattedNumber = format(number, PhoneNumberFormat.INTERNATIONAL).Substring(1);
        break;
      case PhoneNumber.CountryCodeSource.FROM_DEFAULT_COUNTRY:
        // Fall-through to default case.
      default:
        String regionCode = getRegionCodeForCountryCode(number.getCountryCode());
        // We strip non-digits from the NDD here, and from the raw input later, so that we can
        // compare them easily.
        String nationalPrefix = getNddPrefixForRegion(regionCode, true /* strip non-digits */);
        String nationalFormat = format(number, PhoneNumberFormat.NATIONAL);
        if (nationalPrefix == null || nationalPrefix.Length == 0) {
          // If the region doesn't have a national prefix at all, we can safely return the national
          // format without worrying about a national prefix being added.
          formattedNumber = nationalFormat;
          break;
        }
        // Otherwise, we check if the original number was entered with a national prefix.
        if (rawInputContainsNationalPrefix(
            number.getRawInput(), nationalPrefix, regionCode)) {
          // If so, we can safely return the national format.
          formattedNumber = nationalFormat;
          break;
        }
        // Metadata cannot be null here because getNddPrefixForRegion() (above) returns null if
        // there is no metadata for the region.
        PhoneMetadata metadata = getMetadataForRegion(regionCode);
        String nationalNumber = getNationalSignificantNumber(number);
        NumberFormat formatRule =
            chooseFormattingRegexForNumber(metadata.numberFormats(), nationalNumber);
        // The format rule could still be null here if the national number was 0 and there was no
        // raw input (this should not be possible for numbers generated by the phonenumber library
        // as they would also not have a country calling code and we would have exited earlier).
        if (formatRule == null) {
          formattedNumber = nationalFormat;
          break;
        }
        // When the format we apply to this number doesn't contain national prefix, we can just
        // return the national format.
        // TODO: Refactor the code below with the code in
        // isNationalPrefixPresentIfRequired.
        String candidateNationalPrefixRule = formatRule.getNationalPrefixFormattingRule();
        // We assume that the first-group symbol will never be _before_ the national prefix.
        int indexOfFirstGroup = candidateNationalPrefixRule.IndexOf("$1");
        if (indexOfFirstGroup <= 0) {
          formattedNumber = nationalFormat;
          break;
        }
        candidateNationalPrefixRule =
            candidateNationalPrefixRule.Substring(0, indexOfFirstGroup);
        candidateNationalPrefixRule = normalizeDigitsOnly(candidateNationalPrefixRule);
        if (candidateNationalPrefixRule.Length == 0) {
          // National prefix not used when formatting this number.
          formattedNumber = nationalFormat;
          break;
        }
        // Otherwise, we need to remove the national prefix from our output.
        NumberFormat numFormatCopy = new NumberFormat();
        numFormatCopy.mergeFrom(formatRule);
        numFormatCopy.clearNationalPrefixFormattingRule();
        List<NumberFormat> numberFormats = new List<NumberFormat>(1);
        numberFormats.Add(numFormatCopy);
        formattedNumber = formatByPattern(number, PhoneNumberFormat.NATIONAL, numberFormats);
        break;
    }
    String rawInput = number.getRawInput();
    // If no digit is inserted/removed/modified as a result of our formatting, we return the
    // formatted phone number; otherwise we return the raw input the user entered.
    if (formattedNumber != null && rawInput.Length > 0) {
      String normalizedFormattedNumber = normalizeDiallableCharsOnly(formattedNumber);
      String normalizedRawInput = normalizeDiallableCharsOnly(rawInput);
      if (!normalizedFormattedNumber.Equals(normalizedRawInput)) {
        formattedNumber = rawInput;
      }
    }
    return formattedNumber;
  }

  // Check if rawInput, which is assumed to be in the national format, has a national prefix. The
  // national prefix is assumed to be in digits-only form.
  private bool rawInputContainsNationalPrefix(String rawInput, String nationalPrefix,
      String regionCode) {
    String normalizedNationalNumber = normalizeDigitsOnly(rawInput);
    if (normalizedNationalNumber.StartsWith(nationalPrefix)) {
      try {
        // Some Japanese numbers (e.g. 00777123) might be mistaken to contain the national prefix
        // when written without it (e.g. 0777123) if we just do prefix matching. To tackle that, we
        // check the validity of the number if the assumed national prefix is removed (777123 won't
        // be valid in Japan).
        return isValidNumber(
            parse(normalizedNationalNumber.Substring(nationalPrefix.Length), regionCode));
      } catch (NumberParseException) {
        return false;
      }
    }
    return false;
  }

  /**
   * Returns true if a number is from a region whose national significant number couldn't contain a
   * leading zero, but has the italian_leading_zero field set to true.
   */
  private bool hasUnexpectedItalianLeadingZero(PhoneNumber number) {
    return number.isItalianLeadingZero() && !isLeadingZeroPossible(number.getCountryCode());
  }

  private bool hasFormattingRegexForNumber(PhoneNumber number) {
    int countryCallingCode = number.getCountryCode();
    String phoneNumberRegion = getRegionCodeForCountryCode(countryCallingCode);
    PhoneMetadata metadata =
        getMetadataForRegionOrCallingCode(countryCallingCode, phoneNumberRegion);
    if (metadata == null) {
      return false;
    }
    String nationalNumber = getNationalSignificantNumber(number);
    NumberFormat formatRule =
        chooseFormattingRegexForNumber(metadata.numberFormats(), nationalNumber);
    return formatRule != null;
  }

  /**
   * Formats a phone number for out-of-country dialing purposes.
   *
   * Note that in this version, if the number was entered originally using alpha characters and
   * this version of the number is stored in raw_input, this representation of the number will be
   * used rather than the digit representation. Grouping information, as specified by characters
   * such as "-" and " ", will be retained.
   *
   * <p><b>Caveats:</b></p>
   * <ul>
   *  <li> This will not produce good results if the country calling code is both present in the raw
   *       input _and_ is the start of the national number. This is not a problem in the regions
   *       which typically use alpha numbers.
   *  <li> This will also not produce good results if the raw input has any grouping information
   *       within the first three digits of the national number, and if the function needs to strip
   *       preceding digits/words in the raw input before these digits. Normally people group the
   *       first three digits together so this is not a huge problem - and will be fixed if it
   *       proves to be so.
   * </ul>
   *
   * @param number  the phone number that needs to be formatted
   * @param regionCallingFrom  the region where the call is being placed
   * @return  the formatted phone number
   */
  public String formatOutOfCountryKeepingAlphaChars(PhoneNumber number,
                                                    String regionCallingFrom) {
    String rawInput = number.getRawInput();
    // If there is no raw input, then we can't keep alpha characters because there aren't any.
    // In this case, we return formatOutOfCountryCallingNumber.
    if (rawInput.Length == 0) {
      return formatOutOfCountryCallingNumber(number, regionCallingFrom);
    }
    int countryCode = number.getCountryCode();
    if (!hasValidCountryCallingCode(countryCode)) {
      return rawInput;
    }
    // Strip any prefix such as country calling code, IDD, that was present. We do this by comparing
    // the number in raw_input with the parsed number.
    // To do this, first we normalize punctuation. We retain number grouping symbols such as " "
    // only.
    rawInput = normalizeHelper(rawInput, ALL_PLUS_NUMBER_GROUPING_SYMBOLS, true);
    // Now we trim everything before the first three digits in the parsed number. We choose three
    // because all valid alpha numbers have 3 digits at the start - if it does not, then we don't
    // trim anything at all. Similarly, if the national number was less than three digits, we don't
    // trim anything at all.
    String nationalNumber = getNationalSignificantNumber(number);
    if (nationalNumber.Length > 3) {
      int firstNationalNumberDigit = rawInput.IndexOf(nationalNumber.Substring(0, 3), StringComparison.Ordinal);
      if (firstNationalNumberDigit != -1) {
        rawInput = rawInput.Substring(firstNationalNumberDigit);
      }
    }
    PhoneMetadata metadataForRegionCallingFrom = getMetadataForRegion(regionCallingFrom);
    if (countryCode == NANPA_COUNTRY_CODE) {
      if (isNANPACountry(regionCallingFrom)) {
        return countryCode + " " + rawInput;
      }
    } else if (metadataForRegionCallingFrom != null &&
               countryCode == getCountryCodeForValidRegion(regionCallingFrom)) {
      NumberFormat formattingRegex =
          chooseFormattingRegexForNumber(metadataForRegionCallingFrom.numberFormats(),
                                           nationalNumber);
      if (formattingRegex == null) {
        // If no Regex above is matched, we format the original input.
        return rawInput;
      }
      NumberFormat newFormat = new NumberFormat();
      newFormat.mergeFrom(formattingRegex);
      // The first group is the first group of digits that the user wrote together.
      newFormat.setPattern("(\\d+)(.*)");
      // Here we just concatenate them back together after the national prefix has been fixed.
      newFormat.setFormat("$1$2");
      // Now we format using this Regex instead of the default Regex, but with the national
      // prefix prefixed if necessary.
      // This will not work in the cases where the Regex (and not the leading digits) decide
      // whether a national prefix needs to be used, since we have overridden the Regex to match
      // anything, but that is not the case in the metadata to date.
      return formatNsnUsingRegex(rawInput, newFormat, PhoneNumberFormat.NATIONAL);
    }
    String internationalPrefixForFormatting = "";
    // If an unsupported region-calling-from is entered, or a country with multiple international
    // prefixes, the international format of the number is returned, unless there is a preferred
    // international prefix.
    if (metadataForRegionCallingFrom != null) {
      String internationalPrefix = metadataForRegionCallingFrom.getInternationalPrefix();
      internationalPrefixForFormatting =
          UNIQUE_INTERNATIONAL_PREFIX.MatchWhole(internationalPrefix).Success
          ? internationalPrefix
          : metadataForRegionCallingFrom.getPreferredInternationalPrefix();
    }
    StringBuilder formattedNumber = new StringBuilder(rawInput);
    String regionCode = getRegionCodeForCountryCode(countryCode);
    // Metadata cannot be null because the country calling code is valid.
    PhoneMetadata metadataForRegion = getMetadataForRegionOrCallingCode(countryCode, regionCode);
    maybeAppendFormattedExtension(number, metadataForRegion,
                                  PhoneNumberFormat.INTERNATIONAL, formattedNumber);
    if (internationalPrefixForFormatting.Length > 0) {
      formattedNumber.Insert(0, " ").Insert(0, countryCode).Insert(0, " ")
          .Insert(0, internationalPrefixForFormatting);
    } else {
      // Invalid region entered as country-calling-from (so no metadata was found for it) or the
      // region chosen has multiple international dialling prefixes.
      logger.log(Level.WARNING,
                 "Trying to format number from invalid region "
                 + regionCallingFrom
                 + ". International formatting applied.");
      prefixNumberWithCountryCallingCode(countryCode,
                                         PhoneNumberFormat.INTERNATIONAL,
                                         formattedNumber);
    }
    return formattedNumber.ToString();
  }

  /**
   * Gets the national significant number of the a phone number. Note a national significant number
   * doesn't contain a national prefix or any formatting.
   *
   * @param number  the phone number for which the national significant number is needed
   * @return  the national significant number of the PhoneNumber object passed in
   */
  public String getNationalSignificantNumber(PhoneNumber number) {
    // If leading zero(s) have been set, we prefix this now. Note this is not a national prefix.
    StringBuilder nationalNumber = new StringBuilder();
    if (number.isItalianLeadingZero()) {
      char[] zeros = new char[number.getNumberOfLeadingZeros()];
      for (int i = 0; i < zeros.Length; i++) zeros[i] = '0';      
      nationalNumber.Append(new String(zeros));
    }
    nationalNumber.Append(number.getNationalNumber());
    return nationalNumber.ToString();
  }

  /**
   * A helper function that is used by format and formatByRegex.
   */
  private void prefixNumberWithCountryCallingCode(int countryCallingCode,
                                                  PhoneNumberFormat numberFormat,
                                                  StringBuilder formattedNumber) {
    switch (numberFormat) {
      case PhoneNumberFormat.E164:
        formattedNumber.Insert(0, countryCallingCode).Insert(0, PLUS_SIGN);
        return;
      case PhoneNumberFormat.INTERNATIONAL:
        formattedNumber.Insert(0, " ").Insert(0, countryCallingCode).Insert(0, PLUS_SIGN);
        return;
      case PhoneNumberFormat.RFC3966:
        formattedNumber.Insert(0, "-").Insert(0, countryCallingCode).Insert(0, PLUS_SIGN)
            .Insert(0, RFC3966_PREFIX);
        return;
      case PhoneNumberFormat.NATIONAL:
      default:
        return;
    }
  }

  // Simple wrapper of formatNsn for the common case of no carrier code.
  private String formatNsn(String number, PhoneMetadata metadata, PhoneNumberFormat numberFormat) {
    return formatNsn(number, metadata, numberFormat, null);
  }

  // Note in some regions, the national number can be written in two completely different ways
  // depending on whether it forms part of the NATIONAL format or INTERNATIONAL format. The
  // numberFormat parameter here is used to specify which format to use for those cases. If a
  // carrierCode is specified, this will be inserted into the formatted string to replace $CC.
  private String formatNsn(String number,
                           PhoneMetadata metadata,
                           PhoneNumberFormat numberFormat,
                           String carrierCode) {
    List<NumberFormat> intlNumberFormats = metadata.intlNumberFormats();
    // When the intlNumberFormats exists, we use that to format national number for the
    // INTERNATIONAL format instead of using the numberDesc.numberFormats.
    List<NumberFormat> availableFormats =
        (intlNumberFormats.Count == 0 || numberFormat == PhoneNumberFormat.NATIONAL)
        ? metadata.numberFormats()
        : metadata.intlNumberFormats();
    NumberFormat formattingRegex = chooseFormattingRegexForNumber(availableFormats, number);
    return (formattingRegex == null)
        ? number
        : formatNsnUsingRegex(number, formattingRegex, numberFormat, carrierCode);
  }

  internal NumberFormat chooseFormattingRegexForNumber(List<NumberFormat> availableFormats,
                                                String nationalNumber) {
    foreach (NumberFormat numFormat in availableFormats) {
      int size = numFormat.leadingDigitsPatternSize();
      if (size == 0 || regexCache.getRegexForRegex(
              // We always use the last leading_digits_Regex, as it is the most detailed.
              numFormat.getLeadingDigitsPattern(size - 1)).MatchBeginning(nationalNumber).Success) {
        var m = regexCache.getRegexForRegex(numFormat.getPattern()).MatchWhole(nationalNumber);
        if (m.Success) {
          return numFormat;
        }
      }
    }
    return null;
  }

  // Simple wrapper of formatNsnUsingRegex for the common case of no carrier code.
  internal String formatNsnUsingRegex(String nationalNumber,
                               NumberFormat formattingRegex,
                               PhoneNumberFormat numberFormat) {
    return formatNsnUsingRegex(nationalNumber, formattingRegex, numberFormat, null);
  }

  // Note that carrierCode is optional - if null or an empty string, no carrier code replacement
  // will take place.
  private String formatNsnUsingRegex(String nationalNumber,
                                       NumberFormat formattingRegex,
                                       PhoneNumberFormat numberFormat,
                                       String carrierCode) {
    String numberFormatRule = formattingRegex.getFormat();
      var m =
          regexCache.getRegexForRegex(formattingRegex.getPattern());
    String formattedNationalNumber = "";
    if (numberFormat == PhoneNumberFormat.NATIONAL &&
        carrierCode != null && carrierCode.Length > 0 &&
        formattingRegex.getDomesticCarrierCodeFormattingRule().Length > 0) {
      // Replace the $CC in the formatting rule with the desired carrier code.
      String carrierCodeFormattingRule = formattingRegex.getDomesticCarrierCodeFormattingRule();
      carrierCodeFormattingRule =
          CC_PATTERN.Replace(carrierCodeFormattingRule, carrierCode, 1);
      // Now replace the $FG in the formatting rule with the first group and the carrier code
      // combined in the appropriate way.
      numberFormatRule = FIRST_GROUP_PATTERN.Replace(numberFormatRule, carrierCodeFormattingRule, 1);
      formattedNationalNumber = m.Replace(nationalNumber, numberFormatRule);
    } else {
      // Use the national prefix formatting rule instead.
      String nationalPrefixFormattingRule = formattingRegex.getNationalPrefixFormattingRule();
      if (numberFormat == PhoneNumberFormat.NATIONAL &&
          nationalPrefixFormattingRule != null &&
          nationalPrefixFormattingRule.Length > 0) {
        var firstGroup = FIRST_GROUP_PATTERN.Replace(numberFormatRule, nationalPrefixFormattingRule, 1);
        formattedNationalNumber =
            m.Replace(nationalNumber, firstGroup);
      } else {
        formattedNationalNumber = m.Replace(nationalNumber, numberFormatRule);
      }
    }
    if (numberFormat == PhoneNumberFormat.RFC3966) {
      // Strip any leading punctuation.
      var matcher = SEPARATOR_PATTERN;//.matcher(formattedNationalNumber);
      if (matcher.MatchBeginning(formattedNationalNumber).Success) {
        formattedNationalNumber = matcher.Replace(formattedNationalNumber, "", 1);
      }
      // Replace the rest with a dash between each number group.
      formattedNationalNumber = matcher.Replace(formattedNationalNumber, "-");
    }
    return formattedNationalNumber;
  }

  /**
   * Gets a valid number for the specified region.
   *
   * @param regionCode  the region for which an example number is needed
   * @return  a valid fixed-line number for the specified region. Returns null when the metadata
   *    does not contain such information, or the region 001 is passed in. For 001 (representing
   *    non-geographical numbers), call {@link #getExampleNumberForNonGeoEntity} instead.
   */
  public PhoneNumber getExampleNumber(String regionCode) {
    return getExampleNumberForType(regionCode, PhoneNumberType.FIXED_LINE);
  }

  /**
   * Gets a valid number for the specified region and number type.
   *
   * @param regionCode  the region for which an example number is needed
   * @param type  the type of number that is needed
   * @return  a valid number for the specified region and type. Returns null when the metadata
   *     does not contain such information or if an invalid region or region 001 was entered.
   *     For 001 (representing non-geographical numbers), call
   *     {@link #getExampleNumberForNonGeoEntity} instead.
   */
  public PhoneNumber getExampleNumberForType(String regionCode, PhoneNumberType type) {
    // Check the region code is valid.
    if (!isValidRegionCode(regionCode)) {
      logger.log(Level.WARNING, "Invalid or unknown region code provided: " + regionCode);
      return null;
    }
    PhoneNumberDesc desc = getNumberDescByType(getMetadataForRegion(regionCode), type);
    try {
      if (desc.HasExampleNumber()) {
        return parse(desc.getExampleNumber(), regionCode);
      }
    } catch (NumberParseException e) {
      logger.log(Level.SEVERE, e.ToString());
    }
    return null;
  }

  /**
   * Gets a valid number for the specified country calling code for a non-geographical entity.
   *
   * @param countryCallingCode  the country calling code for a non-geographical entity
   * @return  a valid number for the non-geographical entity. Returns null when the metadata
   *    does not contain such information, or the country calling code passed in does not belong
   *    to a non-geographical entity.
   */
  public PhoneNumber getExampleNumberForNonGeoEntity(int countryCallingCode) {
    PhoneMetadata metadata = getMetadataForNonGeographicalRegion(countryCallingCode);
    if (metadata != null) {
      PhoneNumberDesc desc = metadata.getGeneralDesc();
      try {
        if (desc.HasExampleNumber()) {
          return parse("+" + countryCallingCode + desc.getExampleNumber(), "ZZ");
        }
      } catch (NumberParseException e) {
        logger.log(Level.SEVERE, e.ToString());
      }
    } else {
      logger.log(Level.WARNING,
                 "Invalid or unknown country calling code provided: " + countryCallingCode);
    }
    return null;
  }

  /**
   * Appends the formatted extension of a phone number to formattedNumber, if the phone number had
   * an extension specified.
   */
  private void maybeAppendFormattedExtension(PhoneNumber number, PhoneMetadata metadata,
                                             PhoneNumberFormat numberFormat,
                                             StringBuilder formattedNumber) {
    if (number.HasExtension() && number.getExtension().Length > 0) {
      if (numberFormat == PhoneNumberFormat.RFC3966) {
        formattedNumber.Append(RFC3966_EXTN_PREFIX).Append(number.getExtension());
      } else {
        if (metadata.HasPreferredExtnPrefix()) {
          formattedNumber.Append(metadata.getPreferredExtnPrefix()).Append(number.getExtension());
        } else {
          formattedNumber.Append(DEFAULT_EXTN_PREFIX).Append(number.getExtension());
        }
      }
    }
  }

  PhoneNumberDesc getNumberDescByType(PhoneMetadata metadata, PhoneNumberType type) {
    switch (type) {
      case PhoneNumberType.PREMIUM_RATE:
        return metadata.getPremiumRate();
      case PhoneNumberType.TOLL_FREE:
        return metadata.getTollFree();
      case PhoneNumberType.MOBILE:
        return metadata.getMobile();
      case PhoneNumberType.FIXED_LINE:
      case PhoneNumberType.FIXED_LINE_OR_MOBILE:
        return metadata.getFixedLine();
      case PhoneNumberType.SHARED_COST:
        return metadata.getSharedCost();
      case PhoneNumberType.VOIP:
        return metadata.getVoip();
      case PhoneNumberType.PERSONAL_NUMBER:
        return metadata.getPersonalNumber();
      case PhoneNumberType.PAGER:
        return metadata.getPager();
      case PhoneNumberType.UAN:
        return metadata.getUan();
      case PhoneNumberType.VOICEMAIL:
        return metadata.getVoicemail();
      default:
        return metadata.getGeneralDesc();
    }
  }

  /**
   * Gets the type of a phone number.
   *
   * @param number  the phone number that we want to know the type
   * @return  the type of the phone number
   */
  public PhoneNumberType getNumberType(PhoneNumber number) {
    String regionCode = getRegionCodeForNumber(number);
    PhoneMetadata metadata = getMetadataForRegionOrCallingCode(number.getCountryCode(), regionCode);
    if (metadata == null) {
      return PhoneNumberType.UNKNOWN;
    }
    String nationalSignificantNumber = getNationalSignificantNumber(number);
    return getNumberTypeHelper(nationalSignificantNumber, metadata);
  }

  private PhoneNumberType getNumberTypeHelper(String nationalNumber, PhoneMetadata metadata) {
    PhoneNumberDesc generalNumberDesc = metadata.getGeneralDesc();
    if (!isNumberMatchingDesc(nationalNumber, generalNumberDesc)) {
      return PhoneNumberType.UNKNOWN;
    }

    if (isNumberMatchingDesc(nationalNumber, metadata.getPremiumRate())) {
      return PhoneNumberType.PREMIUM_RATE;
    }
    if (isNumberMatchingDesc(nationalNumber, metadata.getTollFree())) {
      return PhoneNumberType.TOLL_FREE;
    }
    if (isNumberMatchingDesc(nationalNumber, metadata.getSharedCost())) {
      return PhoneNumberType.SHARED_COST;
    }
    if (isNumberMatchingDesc(nationalNumber, metadata.getVoip())) {
      return PhoneNumberType.VOIP;
    }
    if (isNumberMatchingDesc(nationalNumber, metadata.getPersonalNumber())) {
      return PhoneNumberType.PERSONAL_NUMBER;
    }
    if (isNumberMatchingDesc(nationalNumber, metadata.getPager())) {
      return PhoneNumberType.PAGER;
    }
    if (isNumberMatchingDesc(nationalNumber, metadata.getUan())) {
      return PhoneNumberType.UAN;
    }
    if (isNumberMatchingDesc(nationalNumber, metadata.getVoicemail())) {
      return PhoneNumberType.VOICEMAIL;
    }

    bool isFixedLine = isNumberMatchingDesc(nationalNumber, metadata.getFixedLine());
    if (isFixedLine) {
      if (metadata.isSameMobileAndFixedLinePattern()) {
        return PhoneNumberType.FIXED_LINE_OR_MOBILE;
      } else if (isNumberMatchingDesc(nationalNumber, metadata.getMobile())) {
        return PhoneNumberType.FIXED_LINE_OR_MOBILE;
      }
      return PhoneNumberType.FIXED_LINE;
    }
    // Otherwise, test to see if the number is mobile. Only do this if certain that the Regexs for
    // mobile and fixed line aren't the same.
    if (!metadata.isSameMobileAndFixedLinePattern() &&
        isNumberMatchingDesc(nationalNumber, metadata.getMobile())) {
      return PhoneNumberType.MOBILE;
    }
    return PhoneNumberType.UNKNOWN;
  }

  /**
   * Returns the metadata for the given region code or {@code null} if the region code is invalid
   * or unknown.
   */
  internal PhoneMetadata getMetadataForRegion(String regionCode) {
    if (!isValidRegionCode(regionCode)) {
      return null;
    }
    lock (regionToMetadataMap) {
      if (!regionToMetadataMap.ContainsKey(regionCode)) {
        // The regionCode here will be valid and won't be '001', so we don't need to worry about
        // what to pass in for the country calling code.
        loadMetadataFromFile(currentFilePrefix, regionCode, 0, metadataLoader);
      }
    }

    PhoneMetadata data = null;
    regionToMetadataMap.TryGetValue(regionCode, out data);
    return data;
  }

  internal PhoneMetadata getMetadataForNonGeographicalRegion(int countryCallingCode) {
    lock (countryCodeToNonGeographicalMetadataMap) {
      if (!countryCallingCodeToRegionCodeMap.ContainsKey(countryCallingCode)) {
        return null;
      }
      if (!countryCodeToNonGeographicalMetadataMap.ContainsKey(countryCallingCode)) {
        loadMetadataFromFile(
            currentFilePrefix, REGION_CODE_FOR_NON_GEO_ENTITY, countryCallingCode, metadataLoader);
      }
    }
    return countryCodeToNonGeographicalMetadataMap[countryCallingCode];
  }

  internal bool isNumberPossibleForDesc(String nationalNumber, PhoneNumberDesc numberDesc) {
    var possibleNumberRegexMatcher =
        regexCache.getRegexForRegex(numberDesc.getPossibleNumberPattern())
            .MatchWhole(nationalNumber);
    return possibleNumberRegexMatcher.Success;
  }

  internal bool isNumberMatchingDesc(String nationalNumber, PhoneNumberDesc numberDesc) {
    var nationalNumberRegexMatcher =
        regexCache.getRegexForRegex(numberDesc.getNationalNumberPattern())
            .MatchWhole(nationalNumber);
    return isNumberPossibleForDesc(nationalNumber, numberDesc) &&
        nationalNumberRegexMatcher.Success;
  }

  /**
   * Tests whether a phone number matches a valid Regex. Note this doesn't verify the number
   * is actually in use, which is impossible to tell by just looking at a number itself.
   *
   * @param number       the phone number that we want to validate
   * @return  a bool that indicates whether the number is of a valid Regex
   */
  public bool isValidNumber(PhoneNumber number) {
    String regionCode = getRegionCodeForNumber(number);
    return isValidNumberForRegion(number, regionCode);
  }

  /**
   * Tests whether a phone number is valid for a certain region. Note this doesn't verify the number
   * is actually in use, which is impossible to tell by just looking at a number itself. If the
   * country calling code is not the same as the country calling code for the region, this
   * immediately exits with false. After this, the specific number Regex rules for the region are
   * examined. This is useful for determining for example whether a particular number is valid for
   * Canada, rather than just a valid NANPA number.
   * Warning: In most cases, you want to use {@link #isValidNumber} instead. For example, this
   * method will mark numbers from British Crown dependencies such as the Isle of Man as invalid for
   * the region "GB" (United Kingdom), since it has its own region code, "IM", which may be
   * undesirable.
   *
   * @param number       the phone number that we want to validate
   * @param regionCode   the region that we want to validate the phone number for
   * @return  a bool that indicates whether the number is of a valid Regex
   */
  public bool isValidNumberForRegion(PhoneNumber number, String regionCode) {
    int countryCode = number.getCountryCode();
    PhoneMetadata metadata = getMetadataForRegionOrCallingCode(countryCode, regionCode);
    if ((metadata == null) ||
        (!REGION_CODE_FOR_NON_GEO_ENTITY.Equals(regionCode) &&
         countryCode != getCountryCodeForValidRegion(regionCode))) {
      // Either the region code was invalid, or the country calling code for this number does not
      // match that of the region code.
      return false;
    }
    String nationalSignificantNumber = getNationalSignificantNumber(number);
    return getNumberTypeHelper(nationalSignificantNumber, metadata) != PhoneNumberType.UNKNOWN;
  }

  /**
   * Returns the region where a phone number is from. This could be used for geocoding at the region
   * level.
   *
   * @param number  the phone number whose origin we want to know
   * @return  the region where the phone number is from, or null if no region matches this calling
   *     code
   */
  public String getRegionCodeForNumber(PhoneNumber number) {
    int countryCode = number.getCountryCode();
    List<String> regions;
    if (!countryCallingCodeToRegionCodeMap.TryGetValue(countryCode, out regions)) {
      String numberString = getNationalSignificantNumber(number);
      logger.log(Level.WARNING,
                 "Missing/invalid country_code (" + countryCode + ") for number " + numberString);
      return null;
    }
    if (regions.Count == 1) {
      return regions[0];
    } else {
      return getRegionCodeForNumberFromRegionList(number, regions);
    }
  }

  private String getRegionCodeForNumberFromRegionList(PhoneNumber number,
                                                      List<String> regionCodes) {
    String nationalNumber = getNationalSignificantNumber(number);
    foreach (String regionCode in regionCodes) {
      // If leadingDigits is present, use this. Otherwise, do full validation.
      // Metadata cannot be null because the region codes come from the country calling code map.
      PhoneMetadata metadata = getMetadataForRegion(regionCode);
      if (metadata.HasLeadingDigits()) {
        if (regexCache.getRegexForRegex(metadata.getLeadingDigits())
                .MatchBeginning(nationalNumber).Success) {
          return regionCode;
        }
      } else if (getNumberTypeHelper(nationalNumber, metadata) != PhoneNumberType.UNKNOWN) {
        return regionCode;
      }
    }
    return null;
  }

  /**
   * Returns the region code that matches the specific country calling code. In the case of no
   * region code being found, ZZ will be returned. In the case of multiple regions, the one
   * designated in the metadata as the "main" region for this calling code will be returned. If the
   * countryCallingCode entered is valid but doesn't match a specific region (such as in the case of
   * non-geographical calling codes like 800) the value "001" will be returned (corresponding to
   * the value for World in the UN M.49 schema).
   */
  public String getRegionCodeForCountryCode(int countryCallingCode) {
    List<String> regionCodes;
    if (countryCallingCodeToRegionCodeMap.TryGetValue(countryCallingCode, out regionCodes))
    {
        return regionCodes[0];
    } else { return UNKNOWN_REGION; }
  }

  /**
   * Returns a list with the region codes that match the specific country calling code. For
   * non-geographical country calling codes, the region code 001 is returned. Also, in the case
   * of no region code being found, an empty list is returned.
   */
  public List<String> getRegionCodesForCountryCode(int countryCallingCode) {
      
    List<String> regionCodes = null;
    if (countryCallingCodeToRegionCodeMap.TryGetValue(countryCallingCode, out regionCodes))
    {
        return regionCodes;
    } 
    else
    {
        return new List<string>(0);
    }    
  }

  /**
   * Returns the country calling code for a specific region. For example, this would be 1 for the
   * United States, and 64 for New Zealand.
   *
   * @param regionCode  the region that we want to get the country calling code for
   * @return  the country calling code for the region denoted by regionCode
   */
  public int getCountryCodeForRegion(String regionCode) {
    if (!isValidRegionCode(regionCode)) {
      logger.log(Level.WARNING,
                 "Invalid or missing region code ("
                  + ((regionCode == null) ? "null" : regionCode)
                  + ") provided.");
      return 0;
    }
    return getCountryCodeForValidRegion(regionCode);
  }

  /**
   * Returns the country calling code for a specific region. For example, this would be 1 for the
   * United States, and 64 for New Zealand. Assumes the region is already valid.
   *
   * @param regionCode  the region that we want to get the country calling code for
   * @return  the country calling code for the region denoted by regionCode
   * @throws IllegalArgumentException if the region is invalid
   */
  private int getCountryCodeForValidRegion(String regionCode) {
    PhoneMetadata metadata = getMetadataForRegion(regionCode);
    if (metadata == null) {
      throw new ArgumentException("Invalid region code: " + regionCode);
    }
    return metadata.getCountryCode();
  }

  /**
   * Returns the national dialling prefix for a specific region. For example, this would be 1 for
   * the United States, and 0 for New Zealand. Set stripNonDigits to true to strip symbols like "~"
   * (which indicates a wait for a dialling tone) from the prefix returned. If no national prefix is
   * present, we return null.
   *
   * <p>Warning: Do not use this method for do-your-own formatting - for some regions, the
   * national dialling prefix is used only for certain types of numbers. Use the library's
   * formatting functions to prefix the national prefix when required.
   *
   * @param regionCode  the region that we want to get the dialling prefix for
   * @param stripNonDigits  true to strip non-digits from the national dialling prefix
   * @return  the dialling prefix for the region denoted by regionCode
   */
  public String getNddPrefixForRegion(String regionCode, bool stripNonDigits) {
    PhoneMetadata metadata = getMetadataForRegion(regionCode);
    if (metadata == null) {
      logger.log(Level.WARNING,
                 "Invalid or missing region code ("
                  + ((regionCode == null) ? "null" : regionCode)
                  + ") provided.");
      return null;
    }
    String nationalPrefix = metadata.getNationalPrefix();
    // If no national prefix was found, we return null.
    if (nationalPrefix.Length == 0) {
      return null;
    }
    if (stripNonDigits) {
      // Note: if any other non-numeric symbols are ever used in national prefixes, these would have
      // to be removed here as well.
      nationalPrefix = nationalPrefix.Replace("~", "");
    }
    return nationalPrefix;
  }

  /**
   * Checks if this is a region under the North American Numbering Plan Administration (NANPA).
   *
   * @return  true if regionCode is one of the regions under NANPA
   */
  public bool isNANPACountry(String regionCode) {
    return nanpaRegions.Contains(regionCode);
  }

  /**
   * Checks whether the country calling code is from a region whose national significant number
   * could contain a leading zero. An example of such a region is Italy. Returns false if no
   * metadata for the country is found.
   */
  internal bool isLeadingZeroPossible(int countryCallingCode) {
    PhoneMetadata mainMetadataForCallingCode =
        getMetadataForRegionOrCallingCode(countryCallingCode,
                                          getRegionCodeForCountryCode(countryCallingCode));
    if (mainMetadataForCallingCode == null) {
      return false;
    }
    return mainMetadataForCallingCode.isLeadingZeroPossible();
  }

  /**
   * Checks if the number is a valid vanity (alpha) number such as 800 MICROSOFT. A valid vanity
   * number will start with at least 3 digits and will have three or more alpha characters. This
   * does not do region-specific checks - to work out if this number is actually valid for a region,
   * it should be parsed and methods such as {@link #isPossibleNumberWithReason} and
   * {@link #isValidNumber} should be used.
   *
   * @param number  the number that needs to be checked
   * @return  true if the number is a valid vanity number
   */
  public bool isAlphaNumber(String number) {
    if (!isViablePhoneNumber(number)) {
      // Number is too short, or doesn't match the basic phone number Regex.
      return false;
    }
    StringBuilder strippedNumber = new StringBuilder(number);
    maybeStripExtension(strippedNumber);
      return VALID_ALPHA_PHONE_PATTERN.MatchWhole(strippedNumber.ToString()).Success;
  }

  /**
   * Convenience wrapper around {@link #isPossibleNumberWithReason}. Instead of returning the reason
   * for failure, this method returns a bool value.
   * @param number  the number that needs to be checked
   * @return  true if the number is possible
   */
  public bool isPossibleNumber(PhoneNumber number) {
    return isPossibleNumberWithReason(number) == ValidationResult.IS_POSSIBLE;
  }

  /**
   * Helper method to check a number against a particular Regex and determine whether it matches,
   * or is too short or too long. Currently, if a number Regex suggests that numbers of length 7
   * and 10 are possible, and a number in between these possible lengths is entered, such as of
   * length 8, this will return TOO_LONG.
   */
  private ValidationResult testNumberLengthAgainstRegex(JavaRegex numberRegex, String number) {
    if (numberRegex.MatchWhole(number).Success) {
      return ValidationResult.IS_POSSIBLE;
    }
    if (numberRegex.MatchBeginning(number).Success) {
      return ValidationResult.TOO_LONG;
    } else {
      return ValidationResult.TOO_SHORT;
    }
  }

  /**
   * Helper method to check whether a number is too short to be a regular length phone number in a
   * region.
   */
  private bool isShorterThanPossibleNormalNumber(PhoneMetadata regionMetadata, String number) {
    JavaRegex possibleNumberRegex = regexCache.getRegexForRegex(
        regionMetadata.getGeneralDesc().getPossibleNumberPattern());
    return testNumberLengthAgainstRegex(possibleNumberRegex, number) ==
        ValidationResult.TOO_SHORT;
  }

  /**
   * Check whether a phone number is a possible number. It provides a more lenient check than
   * {@link #isValidNumber} in the following sense:
   *<ol>
   * <li> It only checks the length of phone numbers. In particular, it doesn't check starting
   *      digits of the number.
   * <li> It doesn't attempt to figure out the type of the number, but uses general rules which
   *      applies to all types of phone numbers in a region. Therefore, it is much faster than
   *      isValidNumber.
   * <li> For fixed line numbers, many regions have the concept of area code, which together with
   *      subscriber number constitute the national significant number. It is sometimes okay to dial
   *      the subscriber number only when dialing in the same area. This function will return
   *      true if the subscriber-number-only version is passed in. On the other hand, because
   *      isValidNumber validates using information on both starting digits (for fixed line
   *      numbers, that would most likely be area codes) and length (obviously includes the
   *      length of area codes for fixed line numbers), it will return false for the
   *      subscriber-number-only version.
   * </ol>
   * @param number  the number that needs to be checked
   * @return  a ValidationResult object which indicates whether the number is possible
   */
  public ValidationResult isPossibleNumberWithReason(PhoneNumber number) {
    String nationalNumber = getNationalSignificantNumber(number);
    int countryCode = number.getCountryCode();
    // Note: For Russian Fed and NANPA numbers, we just use the rules from the default region (US or
    // Russia) since the getRegionCodeForNumber will not work if the number is possible but not
    // valid. This would need to be revisited if the possible number Regex ever differed between
    // various regions within those plans.
    if (!hasValidCountryCallingCode(countryCode)) {
      return ValidationResult.INVALID_COUNTRY_CODE;
    }
    String regionCode = getRegionCodeForCountryCode(countryCode);
    // Metadata cannot be null because the country calling code is valid.
    PhoneMetadata metadata = getMetadataForRegionOrCallingCode(countryCode, regionCode);
    PhoneNumberDesc generalNumDesc = metadata.getGeneralDesc();
    JavaRegex possibleNumberRegex =
        regexCache.getRegexForRegex(generalNumDesc.getPossibleNumberPattern());
    return testNumberLengthAgainstRegex(possibleNumberRegex, nationalNumber);
  }

  /**
   * Check whether a phone number is a possible number given a number in the form of a string, and
   * the region where the number could be dialed from. It provides a more lenient check than
   * {@link #isValidNumber}. See {@link #isPossibleNumber(PhoneNumber)} for details.
   *
   * <p>This method first parses the number, then invokes {@link #isPossibleNumber(PhoneNumber)}
   * with the resultant PhoneNumber object.
   *
   * @param number  the number that needs to be checked, in the form of a string
   * @param regionDialingFrom  the region that we are expecting the number to be dialed from.
   *     Note this is different from the region where the number belongs.  For example, the number
   *     +1 650 253 0000 is a number that belongs to US. When written in this form, it can be
   *     dialed from any region. When it is written as 00 1 650 253 0000, it can be dialed from any
   *     region which uses an international dialling prefix of 00. When it is written as
   *     650 253 0000, it can only be dialed from within the US, and when written as 253 0000, it
   *     can only be dialed from within a smaller area in the US (Mountain View, CA, to be more
   *     specific).
   * @return  true if the number is possible
   */
  public bool isPossibleNumber(String number, String regionDialingFrom) {
    try {
      return isPossibleNumber(parse(number, regionDialingFrom));
    } catch (NumberParseException) {
      return false;
    }
  }

  /**
   * Attempts to extract a valid number from a phone number that is too long to be valid, and resets
   * the PhoneNumber object passed in to that valid version. If no valid number could be extracted,
   * the PhoneNumber object passed in will not be modified.
   * @param number a PhoneNumber object which contains a number that is too long to be valid.
   * @return  true if a valid phone number can be successfully extracted.
   */
  public bool truncateTooLongNumber(PhoneNumber number) {
    if (isValidNumber(number)) {
      return true;
    }
    PhoneNumber numberCopy = new PhoneNumber();
    numberCopy.mergeFrom(number);
    long nationalNumber = number.getNationalNumber();
    do {
      nationalNumber /= 10;
      numberCopy.setNationalNumber(nationalNumber);
      if (isPossibleNumberWithReason(numberCopy) == ValidationResult.TOO_SHORT ||
          nationalNumber == 0) {
        return false;
      }
    } while (!isValidNumber(numberCopy));
    number.setNationalNumber(nationalNumber);
    return true;
  }

  /**
   * Gets an {@link com.google.i18n.phonenumbers.AsYouTypeFormatter} for the specific region.
   *
   * @param regionCode  the region where the phone number is being entered
   * @return  an {@link com.google.i18n.phonenumbers.AsYouTypeFormatter} object, which can be used
   *     to format phone numbers in the specific region "as you type"
   */
  public AsYouTypeFormatter getAsYouTypeFormatter(String regionCode) {
    return new AsYouTypeFormatter(regionCode);
  }

  // Extracts country calling code from fullNumber, returns it and places the remaining number in
  // nationalNumber. It assumes that the leading plus sign or IDD has already been removed. Returns
  // 0 if fullNumber doesn't start with a valid country calling code, and leaves nationalNumber
  // unmodified.
  internal int extractCountryCode(StringBuilder fullNumber, StringBuilder nationalNumber) {
    if ((fullNumber.Length == 0) || (fullNumber[0] == '0')) {
      // Country codes do not begin with a '0'.
      return 0;
    }
    int potentialCountryCode;
    int numberLength = fullNumber.Length;
    for (int i = 1; i <= MAX_LENGTH_COUNTRY_CODE && i <= numberLength; i++) {
      potentialCountryCode = int.Parse(fullNumber.Substring(0, i));
      if (countryCallingCodeToRegionCodeMap.ContainsKey(potentialCountryCode)) {
        nationalNumber.Append(fullNumber.Substring(i));
        return potentialCountryCode;
      }
    }
    return 0;
  }

  /**
   * Tries to extract a country calling code from a number. This method will return zero if no
   * country calling code is considered to be present. Country calling codes are extracted in the
   * following ways:
   * <ul>
   *  <li> by stripping the international dialing prefix of the region the person is dialing from,
   *       if this is present in the number, and looking at the next digits
   *  <li> by stripping the '+' sign if present and then looking at the next digits
   *  <li> by comparing the start of the number and the country calling code of the default region.
   *       If the number is not considered possible for the numbering plan of the default region
   *       initially, but starts with the country calling code of this region, validation will be
   *       reattempted after stripping this country calling code. If this number is considered a
   *       possible number, then the first digits will be considered the country calling code and
   *       removed as such.
   * </ul>
   * It will throw a NumberParseException if the number starts with a '+' but the country calling
   * code supplied after this does not match that of any known region.
   *
   * @param number  non-normalized telephone number that we wish to extract a country calling
   *     code from - may begin with '+'
   * @param defaultRegionMetadata  metadata about the region this number may be from
   * @param nationalNumber  a string buffer to store the national significant number in, in the case
   *     that a country calling code was extracted. The number is appended to any existing contents.
   *     If no country calling code was extracted, this will be left unchanged.
   * @param keepRawInput  true if the country_code_source and preferred_carrier_code fields of
   *     phoneNumber should be populated.
   * @param phoneNumber  the PhoneNumber object where the country_code and country_code_source need
   *     to be populated. Note the country_code is always populated, whereas country_code_source is
   *     only populated when keepCountryCodeSource is true.
   * @return  the country calling code extracted or 0 if none could be extracted
   */
  // @VisibleForTesting
  internal int maybeExtractCountryCode(String number, PhoneMetadata defaultRegionMetadata,
                              StringBuilder nationalNumber, bool keepRawInput,
                              PhoneNumber phoneNumber) {
    if (number.Length == 0) {
      return 0;
    }
    StringBuilder fullNumber = new StringBuilder(number);
    // Set the default prefix to be something that will never match.
    String possibleCountryIddPrefix = "NonMatch";
    if (defaultRegionMetadata != null) {
      possibleCountryIddPrefix = defaultRegionMetadata.getInternationalPrefix();
    }

    PhoneNumber.CountryCodeSource countryCodeSource =
        maybeStripInternationalPrefixAndNormalize(fullNumber, possibleCountryIddPrefix);
    if (keepRawInput) {
      phoneNumber.setCountryCodeSource(countryCodeSource);
    }
    if (countryCodeSource != PhoneNumber.CountryCodeSource.FROM_DEFAULT_COUNTRY) {
      if (fullNumber.Length <= MIN_LENGTH_FOR_NSN) {
        throw new NumberParseException(NumberParseErrorType.TOO_SHORT_AFTER_IDD,
                                       "Phone number had an IDD, but after this was not "
                                       + "long enough to be a viable phone number.");
      }
      int potentialCountryCode = extractCountryCode(fullNumber, nationalNumber);
      if (potentialCountryCode != 0) {
        phoneNumber.setCountryCode(potentialCountryCode);
        return potentialCountryCode;
      }

      // If this fails, they must be using a strange country calling code that we don't recognize,
      // or that doesn't exist.
      throw new NumberParseException(NumberParseErrorType.INVALID_COUNTRY_CODE,
                                     "Country calling code supplied was not recognised.");
    } else if (defaultRegionMetadata != null) {
      // Check to see if the number starts with the country calling code for the default region. If
      // so, we remove the country calling code, and do some checks on the validity of the number
      // before and after.
      int defaultCountryCode = defaultRegionMetadata.getCountryCode();
      String defaultCountryCodeString = defaultCountryCode.ToString();
      String normalizedNumber = fullNumber.ToString();
      if (normalizedNumber.StartsWith(defaultCountryCodeString)) {
        StringBuilder potentialNationalNumber =
            new StringBuilder(normalizedNumber.Substring(defaultCountryCodeString.Length));
        PhoneNumberDesc generalDesc = defaultRegionMetadata.getGeneralDesc();
        JavaRegex validNumberRegex =
            regexCache.getRegexForRegex(generalDesc.getNationalNumberPattern());
        maybeStripNationalPrefixAndCarrierCode(
            potentialNationalNumber, defaultRegionMetadata, null /* Don't need the carrier code */);
        JavaRegex possibleNumberRegex =
            regexCache.getRegexForRegex(generalDesc.getPossibleNumberPattern());
        // If the number was not valid before but is valid now, or if it was too long before, we
        // consider the number with the country calling code stripped to be a better result and
        // keep that instead.
        if ((!validNumberRegex.MatchWhole(fullNumber).Success &&
             validNumberRegex.MatchWhole(potentialNationalNumber).Success) ||
             testNumberLengthAgainstRegex(possibleNumberRegex, fullNumber.ToString())
                  == ValidationResult.TOO_LONG) {
          nationalNumber.Append(potentialNationalNumber);
          if (keepRawInput) {
            phoneNumber.setCountryCodeSource(PhoneNumber.CountryCodeSource.FROM_NUMBER_WITHOUT_PLUS_SIGN);
          }
          phoneNumber.setCountryCode(defaultCountryCode);
          return defaultCountryCode;
        }
      }
    }
    // No country calling code present.
    phoneNumber.setCountryCode(0);
    return 0;
  }

  /**
   * Strips the IDD from the start of the number if present. Helper function used by
   * maybeStripInternationalPrefixAndNormalize.
   */
  private bool parsePrefixAsIdd(JavaRegex iddRegex, StringBuilder number) {
    var m = iddRegex.MatchBeginning(number);
    if (m.Success) {
      int matchEnd = m.Index + m.Length;
      // Only strip this if the first digit after the match is not a 0, since country calling codes
      // cannot begin with 0.
      var digitMatcher = CAPTURING_DIGIT_PATTERN.Match(number.Substring(matchEnd));
      if (digitMatcher.Success) {
        String normalizedGroup = normalizeDigitsOnly(digitMatcher.Groups[1].Value);
        if (normalizedGroup.Equals("0")) {
          return false;
        }
      }
      number.Delete(0, matchEnd);
      return true;
    }
    return false;
  }

  /**
   * Strips any international prefix (such as +, 00, 011) present in the number provided, normalizes
   * the resulting number, and indicates if an international prefix was present.
   *
   * @param number  the non-normalized telephone number that we wish to strip any international
   *     dialing prefix from.
   * @param possibleIddPrefix  the international direct dialing prefix from the region we
   *     think this number may be dialed in
   * @return  the corresponding CountryCodeSource if an international dialing prefix could be
   *     removed from the number, otherwise CountryCodeSource.FROM_DEFAULT_COUNTRY if the number did
   *     not seem to be in international format.
   */
  // @VisibleForTesting
  internal PhoneNumber.CountryCodeSource maybeStripInternationalPrefixAndNormalize(
      StringBuilder number,
      String possibleIddPrefix) {
    if (number.Length == 0) {
      return PhoneNumber.CountryCodeSource.FROM_DEFAULT_COUNTRY;
    }
    // Check to see if the number begins with one or more plus signs.
    var m = PLUS_CHARS_PATTERN.MatchBeginning(number);
    if (m.Success) {
      number.Delete(0, m.Index + m.Length);
      // Can now normalize the rest of the number since we've consumed the "+" sign at the start.
      normalize(number);
      return PhoneNumber.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN;
    }
    // Attempt to parse the first digits as an international prefix.
    var iddRegex = regexCache.getRegexForRegex(possibleIddPrefix);
    normalize(number);
    return parsePrefixAsIdd(iddRegex, number)
           ? PhoneNumber.CountryCodeSource.FROM_NUMBER_WITH_IDD
           : PhoneNumber.CountryCodeSource.FROM_DEFAULT_COUNTRY;
  }

  /**
   * Strips any national prefix (such as 0, 1) present in the number provided.
   *
   * @param number  the normalized telephone number that we wish to strip any national
   *     dialing prefix from
   * @param metadata  the metadata for the region that we think this number is from
   * @param carrierCode  a place to insert the carrier code if one is extracted
   * @return true if a national prefix or carrier code (or both) could be extracted.
   */
  // @VisibleForTesting
  internal bool maybeStripNationalPrefixAndCarrierCode(
      StringBuilder number, PhoneMetadata metadata, StringBuilder carrierCode) {
    int numberLength = number.Length;
    String possibleNationalPrefix = metadata.getNationalPrefixForParsing();
    if (numberLength == 0 || possibleNationalPrefix.Length == 0) {
      // Early return for numbers of zero length.
      return false;
    }
    // Attempt to parse the first digits as a national prefix.
    var prefixMatcher = regexCache.getRegexForRegex(possibleNationalPrefix);
    var prefixMatcherBegin = prefixMatcher.MatchBeginning(number);
    if (prefixMatcherBegin.Success) {
      JavaRegex nationalNumberRule =
          regexCache.getRegexForRegex(metadata.getGeneralDesc().getNationalNumberPattern());
      // Check if the original number is viable.
      bool isViableOriginalNumber = nationalNumberRule.MatchWhole(number).Success;
      // prefixMatcher.group(numOfGroups) == null implies nothing was captured by the capturing
      // groups in possibleNationalPrefix; therefore, no transformation is necessary, and we just
      // remove the national prefix.
      int numOfGroups = prefixMatcherBegin.Groups.Count;
      String transformRule = metadata.getNationalPrefixTransformRule();
      if (transformRule == null || transformRule.Length == 0 ||
          !prefixMatcherBegin.Groups[numOfGroups - 1].Success) {
        var prefixMatcherEnd = prefixMatcherBegin.Index + prefixMatcherBegin.Length;
        // If the original number was viable, and the resultant number is not, we return.
        if (isViableOriginalNumber &&
            !nationalNumberRule.MatchWhole(number.Substring(prefixMatcherEnd)).Success) {
          return false;
        }
        if (carrierCode != null && numOfGroups > 0 && prefixMatcherBegin.Groups[numOfGroups -1].Success) {
          carrierCode.Append(prefixMatcherBegin.Groups[1]);
        }
        number.Delete(0, prefixMatcherEnd);
        return true;
      } else {
        // Check that the resultant number is still viable. If not, return. Check this by copying
        // the string buffer and making the transformation on the copy first.
        StringBuilder transformedNumber = new StringBuilder(number.ToString());
        transformedNumber.Replace(0, numberLength, prefixMatcher.Replace(number.ToString(), transformRule, 1));
        if (isViableOriginalNumber &&
            !nationalNumberRule.MatchWhole(transformedNumber.ToString()).Success) {
          return false;
        }
        if (carrierCode != null && numOfGroups > 1) {
          carrierCode.Append(prefixMatcher.Match(number.ToString()).Groups[1].Value);
        }
        number.Replace(0, number.Length, transformedNumber.ToString());
        return true;
      }
    }
    return false;
  }

  /**
   * Strips any extension (as in, the part of the number dialled after the call is connected,
   * usually indicated with extn, ext, x or similar) from the end of the number, and returns it.
   *
   * @param number  the non-normalized telephone number that we wish to strip the extension from
   * @return        the phone extension
   */
  // @VisibleForTesting
  String maybeStripExtension(StringBuilder number) {
    var m = EXTN_PATTERN.Match(number.ToString());
    // If we find a potential extension, and the number preceding this is a viable number, we assume
    // it is an extension.
    if (m.Success && isViablePhoneNumber(number.Substring(0, m.Index))) {
      // The numbers are captured into groups in the regular expression.
      for (int i = 1, length = m.Groups.Count; i <= length; i++) {
        if (m.Groups[i].Success) {
          // We go through the capturing groups until we find one that captured some digits. If none
          // did, then we will return the empty string.
          String extension = m.Groups[i].Value;
          number.Delete(m.Index, number.Length);
          return extension;
        }
      }
    }
    return "";
  }

  /**
   * Checks to see that the region code used is valid, or if it is not valid, that the number to
   * parse starts with a + symbol so that we can attempt to infer the region from the number.
   * Returns false if it cannot use the region provided and the region cannot be inferred.
   */
  private bool checkRegionForParsing(String numberToParse, String defaultRegion) {
    if (!isValidRegionCode(defaultRegion)) {
      // If the number is null or empty, we can't infer the region.
      if ((numberToParse == null) || (numberToParse.Length == 0) ||
          !PLUS_CHARS_PATTERN.MatchBeginning(numberToParse).Success) {
        return false;
      }
    }
    return true;
  }

  /**
   * Parses a string and returns it in proto buffer format. This method will throw a
   * {@link com.google.i18n.phonenumbers.NumberParseException} if the number is not considered to be
   * a possible number. Note that validation of whether the number is actually a valid number for a
   * particular region is not performed. This can be done separately with {@link #isValidNumber}.
   *
   * @param numberToParse     number that we are attempting to parse. This can contain formatting
   *                          such as +, ( and -, as well as a phone number extension. It can also
   *                          be provided in RFC3966 format.
   * @param defaultRegion     region that we are expecting the number to be from. This is only used
   *                          if the number being parsed is not written in international format.
   *                          The country_code for the number in this case would be stored as that
   *                          of the default region supplied. If the number is guaranteed to
   *                          start with a '+' followed by the country calling code, then
   *                          "ZZ" or null can be supplied.
   * @return                  a phone number proto buffer filled with the parsed number
   * @throws NumberParseException  if the string is not considered to be a viable phone number or if
   *                               no default region was supplied and the number is not in
   *                               international format (does not start with +)
   */
  public PhoneNumber parse(String numberToParse, String defaultRegion) {
    PhoneNumber phoneNumber = new PhoneNumber();
    parse(numberToParse, defaultRegion, phoneNumber);
    return phoneNumber;
  }

  /**
   * Same as {@link #parse(String, String)}, but accepts mutable PhoneNumber as a parameter to
   * decrease object creation when invoked many times.
   */
  public void parse(String numberToParse, String defaultRegion, PhoneNumber phoneNumber) {
    parseHelper(numberToParse, defaultRegion, false, true, phoneNumber);
  }

  /**
   * Parses a string and returns it in proto buffer format. This method differs from {@link #parse}
   * in that it always populates the raw_input field of the protocol buffer with numberToParse as
   * well as the country_code_source field.
   *
   * @param numberToParse     number that we are attempting to parse. This can contain formatting
   *                          such as +, ( and -, as well as a phone number extension.
   * @param defaultRegion     region that we are expecting the number to be from. This is only used
   *                          if the number being parsed is not written in international format.
   *                          The country calling code for the number in this case would be stored
   *                          as that of the default region supplied.
   * @return                  a phone number proto buffer filled with the parsed number
   * @throws NumberParseException  if the string is not considered to be a viable phone number or if
   *                               no default region was supplied
   */
  public PhoneNumber parseAndKeepRawInput(String numberToParse, String defaultRegion) {
    PhoneNumber phoneNumber = new PhoneNumber();
    parseAndKeepRawInput(numberToParse, defaultRegion, phoneNumber);
    return phoneNumber;
  }

  /**
   * Same as{@link #parseAndKeepRawInput(String, String)}, but accepts a mutable PhoneNumber as
   * a parameter to decrease object creation when invoked many times.
   */
  public void parseAndKeepRawInput(String numberToParse, String defaultRegion,
                                   PhoneNumber phoneNumber) {
    parseHelper(numberToParse, defaultRegion, true, true, phoneNumber);
  }

  /**
   * Returns an iterable over all {@link PhoneNumberMatch PhoneNumberMatches} in {@code text}. This
   * is a shortcut for {@link #findNumbers(CharSequence, String, Leniency, long)
   * getMatcher(text, defaultRegion, Leniency.VALID, Long.MAX_VALUE)}.
   *
   * @param text              the text to search for phone numbers, null for no text
   * @param defaultRegion     region that we are expecting the number to be from. This is only used
   *                          if the number being parsed is not written in international format. The
   *                          country_code for the number in this case would be stored as that of
   *                          the default region supplied. May be null if only international
   *                          numbers are expected.
   */
  public IEnumerator<PhoneNumberMatch> findNumbers(string text, String defaultRegion) {
    return findNumbers(text, defaultRegion, Leniency.VALID, long.MaxValue);
  }

  /**
   * Returns an iterable over all {@link PhoneNumberMatch PhoneNumberMatches} in {@code text}.
   *
   * @param text              the text to search for phone numbers, null for no text
   * @param defaultRegion     region that we are expecting the number to be from. This is only used
   *                          if the number being parsed is not written in international format. The
   *                          country_code for the number in this case would be stored as that of
   *                          the default region supplied. May be null if only international
   *                          numbers are expected.
   * @param leniency          the leniency to use when evaluating candidate phone numbers
   * @param maxTries          the maximum number of invalid numbers to try before giving up on the
   *                          text. This is to cover degenerate cases where the text has a lot of
   *                          false positives in it. Must be {@code >= 0}.
   */
  public IEnumerator<PhoneNumberMatch> findNumbers(
      string text, String defaultRegion, Leniency leniency, long maxTries) {
    return new PhoneNumberMatcher(this, text, defaultRegion, leniency, maxTries);
  }

  /**
   * A helper function to set the values related to leading zeros in a PhoneNumber.
   */
  static void setItalianLeadingZerosForPhoneNumber(String nationalNumber, PhoneNumber phoneNumber) {
    if (nationalNumber.Length > 1 && nationalNumber[0] == '0') {
      phoneNumber.setItalianLeadingZero(true);
      int numberOfLeadingZeros = 1;
      // Note that if the national number is all "0"s, the last "0" is not counted as a leading
      // zero.
      while (numberOfLeadingZeros < nationalNumber.Length - 1 &&
             nationalNumber[numberOfLeadingZeros] == '0') {
        numberOfLeadingZeros++;
      }
      if (numberOfLeadingZeros != 1) {
        phoneNumber.setNumberOfLeadingZeros(numberOfLeadingZeros);
      }
    }
  }

  /**
   * Parses a string and fills up the phoneNumber. This method is the same as the public
   * parse() method, with the exception that it allows the default region to be null, for use by
   * isNumberMatch(). checkRegion should be set to false if it is permitted for the default region
   * to be null or unknown ("ZZ").
   */
  private void parseHelper(String numberToParse, String defaultRegion, bool keepRawInput,
                           bool checkRegion, PhoneNumber phoneNumber)  {
    if (numberToParse == null) {
      throw new NumberParseException(NumberParseErrorType.NOT_A_NUMBER,
                                     "The phone number supplied was null.");
    } else if (numberToParse.Length > MAX_INPUT_STRING_LENGTH) {
      throw new NumberParseException(NumberParseErrorType.TOO_LONG,
                                     "The string supplied was too long to parse.");
    }

    StringBuilder nationalNumber = new StringBuilder();
    buildNationalNumberForParsing(numberToParse, nationalNumber);

    if (!isViablePhoneNumber(nationalNumber.ToString())) {
      throw new NumberParseException(NumberParseErrorType.NOT_A_NUMBER,
                                     "The string supplied did not seem to be a phone number.");
    }

    // Check the region supplied is valid, or that the extracted number starts with some sort of +
    // sign so the number's region can be determined.
    if (checkRegion && !checkRegionForParsing(nationalNumber.ToString(), defaultRegion)) {
      throw new NumberParseException(NumberParseErrorType.INVALID_COUNTRY_CODE,
                                     "Missing or invalid default region.");
    }

    if (keepRawInput) {
      phoneNumber.setRawInput(numberToParse);
    }
    // Attempt to parse extension first, since it doesn't require region-specific data and we want
    // to have the non-normalised number here.
    String extension = maybeStripExtension(nationalNumber);
    if (extension.Length > 0) {
      phoneNumber.setExtension(extension);
    }

    PhoneMetadata regionMetadata = getMetadataForRegion(defaultRegion);
    // Check to see if the number is given in international format so we know whether this number is
    // from the default region or not.
    StringBuilder normalizedNationalNumber = new StringBuilder();
    int countryCode = 0;
    try {
      // TODO: This method should really just take in the string buffer that has already
      // been created, and just remove the prefix, rather than taking in a string and then
      // outputting a string buffer.
      countryCode = maybeExtractCountryCode(nationalNumber.ToString(), regionMetadata,
                                            normalizedNationalNumber, keepRawInput, phoneNumber);
    } catch (NumberParseException e) {
      var matcher = PLUS_CHARS_PATTERN.MatchBeginning(nationalNumber.ToString());
      if (e.getErrorType() == NumberParseErrorType.INVALID_COUNTRY_CODE &&
          matcher.Success) {
        // Strip the plus-char, and try again.
        countryCode = maybeExtractCountryCode(nationalNumber.Substring(matcher.Index + matcher.Length),
                                              regionMetadata, normalizedNationalNumber,
                                              keepRawInput, phoneNumber);
        if (countryCode == 0) {
          throw new NumberParseException(NumberParseErrorType.INVALID_COUNTRY_CODE,
                                         "Could not interpret numbers after plus-sign.");
        }
      } else {
        throw new NumberParseException(e.getErrorType(), e.Message);
      }
    }
    if (countryCode != 0) {
      String phoneNumberRegion = getRegionCodeForCountryCode(countryCode);
      if (!phoneNumberRegion.Equals(defaultRegion)) {
        // Metadata cannot be null because the country calling code is valid.
        regionMetadata = getMetadataForRegionOrCallingCode(countryCode, phoneNumberRegion);
      }
    } else {
      // If no extracted country calling code, use the region supplied instead. The national number
      // is just the normalized version of the number we were given to parse.
      normalize(nationalNumber);
      normalizedNationalNumber.Append(nationalNumber);
      if (defaultRegion != null) {
        countryCode = regionMetadata.getCountryCode();
        phoneNumber.setCountryCode(countryCode);
      } else if (keepRawInput) {
        phoneNumber.clearCountryCodeSource();
      }
    }
    if (normalizedNationalNumber.Length < MIN_LENGTH_FOR_NSN) {
      throw new NumberParseException(NumberParseErrorType.TOO_SHORT_NSN,
                                     "The string supplied is too short to be a phone number.");
    }
    if (regionMetadata != null) {
      StringBuilder carrierCode = new StringBuilder();
      StringBuilder potentialNationalNumber = new StringBuilder(normalizedNationalNumber.ToString());
      maybeStripNationalPrefixAndCarrierCode(potentialNationalNumber, regionMetadata, carrierCode);
      // We require that the NSN remaining after stripping the national prefix and carrier code be
      // of a possible length for the region. Otherwise, we don't do the stripping, since the
      // original number could be a valid short number.
      if (!isShorterThanPossibleNormalNumber(regionMetadata, potentialNationalNumber.ToString())) {
        normalizedNationalNumber = potentialNationalNumber;
        if (keepRawInput) {
          phoneNumber.setPreferredDomesticCarrierCode(carrierCode.ToString());
        }
      }
    }
    int lengthOfNationalNumber = normalizedNationalNumber.Length;
    if (lengthOfNationalNumber < MIN_LENGTH_FOR_NSN) {
      throw new NumberParseException(NumberParseErrorType.TOO_SHORT_NSN,
                                     "The string supplied is too short to be a phone number.");
    }
    if (lengthOfNationalNumber > MAX_LENGTH_FOR_NSN) {
      throw new NumberParseException(NumberParseErrorType.TOO_LONG,
                                     "The string supplied is too long to be a phone number.");
    }
    setItalianLeadingZerosForPhoneNumber(normalizedNationalNumber.ToString(), phoneNumber);
    phoneNumber.setNationalNumber(long.Parse(normalizedNationalNumber.ToString()));
  }

  /**
   * Converts numberToParse to a form that we can parse and write it to nationalNumber if it is
   * written in RFC3966; otherwise extract a possible number out of it and write to nationalNumber.
   */
  private void buildNationalNumberForParsing(String numberToParse, StringBuilder nationalNumber) {
    int indexOfPhoneContext = numberToParse.IndexOf(RFC3966_PHONE_CONTEXT, StringComparison.Ordinal);
    if (indexOfPhoneContext > 0) {
      int phoneContextStart = indexOfPhoneContext + RFC3966_PHONE_CONTEXT.Length;
      // If the phone context contains a phone number prefix, we need to capture it, whereas domains
      // will be ignored.
      if (numberToParse[phoneContextStart] == PLUS_SIGN) {
        // Additional parameters might follow the phone context. If so, we will remove them here
        // because the parameters after phone context are not important for parsing the
        // phone number.
        int phoneContextEnd = numberToParse.IndexOf(';', phoneContextStart);
        if (phoneContextEnd > 0) {
          nationalNumber.Append(numberToParse.Substring(phoneContextStart, phoneContextEnd - phoneContextStart));
        } else {
          nationalNumber.Append(numberToParse.Substring(phoneContextStart));
        }
      }

      // Now append everything between the "tel:" prefix and the phone-context. This should include
      // the national number, an optional extension or isdn-subaddress component. Note we also
      // handle the case when "tel:" is missing, as we have seen in some of the phone number inputs.
      // In that case, we append everything from the beginning.
      int indexOfRfc3966Prefix = numberToParse.IndexOf(RFC3966_PREFIX);
      int indexOfNationalNumber = (indexOfRfc3966Prefix >= 0) ?
          indexOfRfc3966Prefix + RFC3966_PREFIX.Length : 0;
      nationalNumber.Append(numberToParse.Substring(indexOfNationalNumber, indexOfPhoneContext - indexOfNationalNumber));
    } else {
      // Extract a possible number from the string passed in (this strips leading characters that
      // could not be the start of a phone number.)
      nationalNumber.Append(extractPossibleNumber(numberToParse));
    }

    // Delete the isdn-subaddress and everything after it if it is present. Note extension won't
    // appear at the same time with isdn-subaddress according to paragraph 5.3 of the RFC3966 spec,
    int indexOfIsdn = nationalNumber.ToString().IndexOf(RFC3966_ISDN_SUBADDRESS, StringComparison.Ordinal);
    if (indexOfIsdn > 0) {
      nationalNumber.Delete(indexOfIsdn, nationalNumber.Length);
    }
    // If both phone context and isdn-subaddress are absent but other parameters are present, the
    // parameters are left in nationalNumber. This is because we are concerned about deleting
    // content from a potential number string when there is no strong evidence that the number is
    // actually written in RFC3966.
  }

  /**
   * Takes two phone numbers and compares them for equality.
   *
   * <p>Returns EXACT_MATCH if the country_code, NSN, presence of a leading zero for Italian numbers
   * and any extension present are the same.
   * Returns NSN_MATCH if either or both has no region specified, and the NSNs and extensions are
   * the same.
   * Returns SHORT_NSN_MATCH if either or both has no region specified, or the region specified is
   * the same, and one NSN could be a shorter version of the other number. This includes the case
   * where one has an extension specified, and the other does not.
   * Returns NO_MATCH otherwise.
   * For example, the numbers +1 345 657 1234 and 657 1234 are a SHORT_NSN_MATCH.
   * The numbers +1 345 657 1234 and 345 657 are a NO_MATCH.
   *
   * @param firstNumberIn  first number to compare
   * @param secondNumberIn  second number to compare
   *
   * @return  NO_MATCH, SHORT_NSN_MATCH, NSN_MATCH or EXACT_MATCH depending on the level of equality
   *     of the two numbers, described in the method definition.
   */
  public MatchType isNumberMatch(PhoneNumber firstNumberIn, PhoneNumber secondNumberIn) {
    // Make copies of the phone number so that the numbers passed in are not edited.
    PhoneNumber firstNumber = new PhoneNumber();
    firstNumber.mergeFrom(firstNumberIn);
    PhoneNumber secondNumber = new PhoneNumber();
    secondNumber.mergeFrom(secondNumberIn);
    // First clear raw_input, country_code_source and preferred_domestic_carrier_code fields and any
    // empty-string extensions so that we can use the proto-buffer equality method.
    firstNumber.clearRawInput();
    firstNumber.clearCountryCodeSource();
    firstNumber.clearPreferredDomesticCarrierCode();
    secondNumber.clearRawInput();
    secondNumber.clearCountryCodeSource();
    secondNumber.clearPreferredDomesticCarrierCode();
    if (firstNumber.HasExtension() &&
        firstNumber.getExtension().Length == 0) {
        firstNumber.clearExtension();
    }
    if (secondNumber.HasExtension() &&
        secondNumber.getExtension().Length == 0) {
        secondNumber.clearExtension();
    }
    // Early exit if both had extensions and these are different.
    if (firstNumber.HasExtension() && secondNumber.HasExtension() &&
        !firstNumber.getExtension().Equals(secondNumber.getExtension())) {
      return MatchType.NO_MATCH;
    }
    int firstNumberCountryCode = firstNumber.getCountryCode();
    int secondNumberCountryCode = secondNumber.getCountryCode();
    // Both had country_code specified.
    if (firstNumberCountryCode != 0 && secondNumberCountryCode != 0) {
      if (firstNumber.exactlySameAs(secondNumber)) {
        return MatchType.EXACT_MATCH;
      } else if (firstNumberCountryCode == secondNumberCountryCode &&
                 isNationalNumberSuffixOfTheOther(firstNumber, secondNumber)) {
        // A SHORT_NSN_MATCH occurs if there is a difference because of the presence or absence of
        // an 'Italian leading zero', the presence or absence of an extension, or one NSN being a
        // shorter variant of the other.
        return MatchType.SHORT_NSN_MATCH;
      }
      // This is not a match.
      return MatchType.NO_MATCH;
    }
    // Checks cases where one or both country_code fields were not specified. To make equality
    // checks easier, we first set the country_code fields to be equal.
    firstNumber.setCountryCode(secondNumberCountryCode);
    // If all else was the same, then this is an NSN_MATCH.
    if (firstNumber.exactlySameAs(secondNumber)) {
      return MatchType.NSN_MATCH;
    }
    if (isNationalNumberSuffixOfTheOther(firstNumber, secondNumber)) {
      return MatchType.SHORT_NSN_MATCH;
    }
    return MatchType.NO_MATCH;
  }

  // Returns true when one national number is the suffix of the other or both are the same.
  private bool isNationalNumberSuffixOfTheOther(PhoneNumber firstNumber,
                                                   PhoneNumber secondNumber) {
    String firstNumberNationalNumber = firstNumber.getNationalNumber().ToString();
    String secondNumberNationalNumber = secondNumber.getNationalNumber().ToString();
    // Note that endsWith returns true if the numbers are equal.
    return firstNumberNationalNumber.EndsWith(secondNumberNationalNumber) ||
           secondNumberNationalNumber.EndsWith(firstNumberNationalNumber);
  }

  /**
   * Takes two phone numbers as strings and compares them for equality. This is a convenience
   * wrapper for {@link #isNumberMatch(PhoneNumber, PhoneNumber)}. No default region is known.
   *
   * @param firstNumber  first number to compare. Can contain formatting, and can have country
   *     calling code specified with + at the start.
   * @param secondNumber  second number to compare. Can contain formatting, and can have country
   *     calling code specified with + at the start.
   * @return  NOT_A_NUMBER, NO_MATCH, SHORT_NSN_MATCH, NSN_MATCH, EXACT_MATCH. See
   *     {@link #isNumberMatch(PhoneNumber, PhoneNumber)} for more details.
   */
  public MatchType isNumberMatch(String firstNumber, String secondNumber) {
    try {
      PhoneNumber firstNumberAsProto = parse(firstNumber, UNKNOWN_REGION);
      return isNumberMatch(firstNumberAsProto, secondNumber);
    } catch (NumberParseException e) {
      if (e.getErrorType() == NumberParseErrorType.INVALID_COUNTRY_CODE) {
        try {
          PhoneNumber secondNumberAsProto = parse(secondNumber, UNKNOWN_REGION);
          return isNumberMatch(secondNumberAsProto, firstNumber);
        } catch (NumberParseException e2) {
          if (e2.getErrorType() == NumberParseErrorType.INVALID_COUNTRY_CODE) {
            try {
              PhoneNumber firstNumberProto = new PhoneNumber();
              PhoneNumber secondNumberProto = new PhoneNumber();
              parseHelper(firstNumber, null, false, false, firstNumberProto);
              parseHelper(secondNumber, null, false, false, secondNumberProto);
              return isNumberMatch(firstNumberProto, secondNumberProto);
            } catch (NumberParseException) {
              // Fall through and return MatchType.NOT_A_NUMBER.
            }
          }
        }
      }
    }
    // One or more of the phone numbers we are trying to match is not a viable phone number.
    return MatchType.NOT_A_NUMBER;
  }

  /**
   * Takes two phone numbers and compares them for equality. This is a convenience wrapper for
   * {@link #isNumberMatch(PhoneNumber, PhoneNumber)}. No default region is known.
   *
   * @param firstNumber  first number to compare in proto buffer format.
   * @param secondNumber  second number to compare. Can contain formatting, and can have country
   *     calling code specified with + at the start.
   * @return  NOT_A_NUMBER, NO_MATCH, SHORT_NSN_MATCH, NSN_MATCH, EXACT_MATCH. See
   *     {@link #isNumberMatch(PhoneNumber, PhoneNumber)} for more details.
   */
  public MatchType isNumberMatch(PhoneNumber firstNumber, String secondNumber) {
    // First see if the second number has an implicit country calling code, by attempting to parse
    // it.
    try {
      PhoneNumber secondNumberAsProto = parse(secondNumber, UNKNOWN_REGION);
      return isNumberMatch(firstNumber, secondNumberAsProto);
    } catch (NumberParseException e) {
      if (e.getErrorType() == NumberParseErrorType.INVALID_COUNTRY_CODE) {
        // The second number has no country calling code. EXACT_MATCH is no longer possible.
        // We parse it as if the region was the same as that for the first number, and if
        // EXACT_MATCH is returned, we replace this with NSN_MATCH.
        String firstNumberRegion = getRegionCodeForCountryCode(firstNumber.getCountryCode());
        try {
          if (!firstNumberRegion.Equals(UNKNOWN_REGION)) {
            PhoneNumber secondNumberWithFirstNumberRegion = parse(secondNumber, firstNumberRegion);
            MatchType match = isNumberMatch(firstNumber, secondNumberWithFirstNumberRegion);
            if (match == MatchType.EXACT_MATCH) {
              return MatchType.NSN_MATCH;
            }
            return match;
          } else {
            // If the first number didn't have a valid country calling code, then we parse the
            // second number without one as well.
            PhoneNumber secondNumberProto = new PhoneNumber();
            parseHelper(secondNumber, null, false, false, secondNumberProto);
            return isNumberMatch(firstNumber, secondNumberProto);
          }
        } catch (NumberParseException) {
          // Fall-through to return NOT_A_NUMBER.
        }
      }
    }
    // One or more of the phone numbers we are trying to match is not a viable phone number.
    return MatchType.NOT_A_NUMBER;
  }

  /**
   * Returns true if the number can be dialled from outside the region, or unknown. If the number
   * can only be dialled from within the region, returns false. Does not check the number is a valid
   * number. Note that, at the moment, this method does not handle short numbers.
   * TODO: Make this method public when we have enough metadata to make it worthwhile.
   *
   * @param number  the phone-number for which we want to know whether it is diallable from
   *     outside the region
   */
  // @VisibleForTesting
  internal bool canBeInternationallyDialled(PhoneNumber number) {
    PhoneMetadata metadata = getMetadataForRegion(getRegionCodeForNumber(number));
    if (metadata == null) {
      // Note numbers belonging to non-geographical entities (e.g. +800 numbers) are always
      // internationally diallable, and will be caught here.
      return true;
    }
    String nationalSignificantNumber = getNationalSignificantNumber(number);
    return !isNumberMatchingDesc(nationalSignificantNumber, metadata.getNoInternationalDialling());
  }

  /**
   * Returns true if the supplied region supports mobile number portability. Returns false for
   * invalid, unknown or regions that don't support mobile number portability.
   *
   * @param regionCode  the region for which we want to know whether it supports mobile number
   *                    portability or not.
   */
  public bool isMobileNumberPortableRegion(String regionCode) {
    PhoneMetadata metadata = getMetadataForRegion(regionCode);
    if (metadata == null) {
      logger.log(Level.WARNING, "Invalid or unknown region code provided: " + regionCode);
      return false;
    }
    return metadata.isMobileNumberPortableRegion();
  }
}
}
