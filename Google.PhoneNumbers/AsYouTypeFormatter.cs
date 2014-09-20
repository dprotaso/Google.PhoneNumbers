/*
 * Copyright (C) 2009 The Libphonenumber Authors
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
using System.Text;
using System.Text.RegularExpressions;

namespace Google.PhoneNumbers
{
    public class AsYouTypeFormatter
    {
        private String currentOutput = "";
        private StringBuilder formattingTemplate = new StringBuilder();
        // The pattern from numberFormat that is currently used to create formattingTemplate.
        private String currentFormattingPattern = "";
        private StringBuilder accruedInput = new StringBuilder();
        private StringBuilder accruedInputWithoutFormatting = new StringBuilder();
        // This indicates whether AsYouTypeFormatter is currently doing the formatting.
        private bool ableToFormat = true;
        // Set to true when users enter their own formatting. AsYouTypeFormatter will do no formatting at
        // all when this is set to true.
        private bool inputHasFormatting = false;
        // This is set to true when we know the user is entering a full national significant number, since
        // we have either detected a national prefix or an international dialing prefix. When this is
        // true, we will no longer use local number formatting patterns.
        private bool isCompleteNumber = false;
        private bool isExpectingCountryCallingCode = false;
        private readonly PhoneNumberUtil phoneUtil = PhoneNumberUtil.getInstance();
        private String defaultCountry;

        // Character used when appropriate to separate a prefix, such as a long NDD or a country calling
        // code, from the national number.
        private static readonly char SEPARATOR_BEFORE_NATIONAL_NUMBER = ' ';

        private static readonly PhoneMetadata EMPTY_METADATA =
            new PhoneMetadata().setInternationalPrefix("NA");

        private PhoneMetadata defaultMetadata;
        private PhoneMetadata currentMetadata;

        // A pattern that is used to match character classes in regular expressions. An example of a
        // character class is [1-4].
        private static readonly Regex CHARACTER_CLASS_PATTERN = new Regex("\\[([^\\[\\]])*\\]");
        // Any digit in a regular expression that actually denotes a digit. For example, in the regular
        // expression 80[0-2]\d{6,10}, the first 2 digits (8 and 0) are standalone digits, but the rest
        // are not.
        // Two look-aheads are needed because the number following \\d could be a two-digit number, since
        // the phone number can be as long as 15 digits.
        private static readonly Regex STANDALONE_DIGIT_PATTERN = new Regex("\\d(?=[^,}][^,}])");

        // A pattern that is used to determine if a numberFormat under availableFormats is eligible to be
        // used by the AYTF. It is eligible when the format element under numberFormat contains groups of
        // the dollar sign followed by a single digit, separated by valid phone number punctuation. This
        // prevents invalid punctuation (such as the star sign in Israeli star numbers) getting into the
        // output of the AYTF.
        private static readonly JavaRegex ELIGIBLE_FORMAT_PATTERN =
            new JavaRegex("[" + PhoneNumberUtil.VALID_PUNCTUATION + "]*" +
                          "(\\$\\d" + "[" + PhoneNumberUtil.VALID_PUNCTUATION + "]*)+");

        // A set of characters that, if found in a national prefix formatting rules, are an indicator to
        // us that we should separate the national prefix from the number when formatting.
        private static readonly Regex NATIONAL_PREFIX_SEPARATORS_PATTERN = new Regex("[- ]");

        // This is the minimum length of national number accrued that is required to trigger the
        // formatter. The first element of the leadingDigitsRegex of each numberFormat contains a
        // regular expression that matches up to this number of digits.
        private static readonly int MIN_LEADING_DIGITS_LENGTH = 3;

        // The digits that have not been entered yet will be represented by a \u2008, the punctuation
        // space.
        private static readonly String DIGIT_PLACEHOLDER = "\u2008";
        private static readonly Regex DIGIT_PATTERN = new Regex(DIGIT_PLACEHOLDER);
        private int lastMatchPosition = 0;
        // The position of a digit upon which inputDigitAndRememberPosition is most recently invoked, as
        // found in the original sequence of characters the user entered.
        private int originalPosition = 0;
        // The position of a digit upon which inputDigitAndRememberPosition is most recently invoked, as
        // found in accruedInputWithoutFormatting.
        private int positionToRemember = 0;
        // This contains anything that has been entered so far preceding the national significant number,
        // and it is formatted (e.g. with space inserted). For example, this can contain IDD, country
        // code, and/or NDD, etc.
        private StringBuilder prefixBeforeNationalNumber = new StringBuilder();
        private bool shouldAddSpaceAfterNationalPrefix = false;
        // This contains the national prefix that has been extracted. It contains only digits without
        // formatting.
        private String extractedNationalPrefix = "";
        private StringBuilder nationalNumber = new StringBuilder();
        private List<NumberFormat> possibleFormats = new List<NumberFormat>();

        // A cache for frequently used country-specific regular expressions.
        private RegexCache regexCache = new RegexCache(64);

        /**
   * Constructs an as-you-type formatter. Should be obtained from {@link
   * PhoneNumberUtil#getAsYouTypeFormatter}.
   *
   * @param regionCode  the country/region where the phone number is being entered
   */

        internal AsYouTypeFormatter(String regionCode)
        {
            defaultCountry = regionCode;
            currentMetadata = getMetadataForRegion(defaultCountry);
            defaultMetadata = currentMetadata;
        }

        // The metadata needed by this class is the same for all regions sharing the same country calling
        // code. Therefore, we return the metadata for "main" region for this country calling code.
        private PhoneMetadata getMetadataForRegion(String regionCode)
        {
            int countryCallingCode = phoneUtil.getCountryCodeForRegion(regionCode);
            String mainCountry = phoneUtil.getRegionCodeForCountryCode(countryCallingCode);
            PhoneMetadata metadata = phoneUtil.getMetadataForRegion(mainCountry);
            if (metadata != null)
            {
                return metadata;
            }
            // Set to a default instance of the metadata. This allows us to function with an incorrect
            // region code, even if formatting only works for numbers specified with "+".
            return EMPTY_METADATA;
        }

        // Returns true if a new template is created as opposed to reusing the existing template.
        private bool maybeCreateNewTemplate()
        {
            // When there are multiple available formats, the formatter uses the first format where a
            // formatting template could be created.
            foreach (var numberFormat in possibleFormats.ToArray())
            {
                String pattern = numberFormat.getPattern();
                if (currentFormattingPattern.Equals(pattern))
                {
                    return false;
                }
                if (createFormattingTemplate(numberFormat))
                {
                    currentFormattingPattern = pattern;
                    shouldAddSpaceAfterNationalPrefix =
                        NATIONAL_PREFIX_SEPARATORS_PATTERN.Match(
                            numberFormat.getNationalPrefixFormattingRule()).Success;
                    // With a new formatting template, the matched position using the old template needs to be
                    // reset.
                    lastMatchPosition = 0;
                    return true;
                }
                else
                {
                    // Remove the current number format from possibleFormats.
                    possibleFormats.Remove(numberFormat);
                }
            }
            ableToFormat = false;
            return false;
        }

        private void getAvailableFormats(String leadingDigits)
        {
            List<NumberFormat> formatList =
                (isCompleteNumber && currentMetadata.IntlNumberFormatSize() > 0)
                    ? currentMetadata.intlNumberFormats()
                    : currentMetadata.numberFormats();
            bool nationalPrefixIsUsedByCountry = currentMetadata.HasNationalPrefix();
            foreach (NumberFormat format in formatList)
            {
                if (!nationalPrefixIsUsedByCountry || isCompleteNumber ||
                    format.isNationalPrefixOptionalWhenFormatting() ||
                    PhoneNumberUtil.formattingRuleHasFirstGroupOnly(
                        format.getNationalPrefixFormattingRule()))
                {
                    if (isFormatEligible(format.getFormat()))
                    {
                        possibleFormats.Add(format);
                    }
                }
            }
            narrowDownPossibleFormats(leadingDigits);
        }

        private bool isFormatEligible(String format)
        {
            return ELIGIBLE_FORMAT_PATTERN.MatchWhole(format).Success;
        }

        private void narrowDownPossibleFormats(String leadingDigits)
        {
            int indexOfLeadingDigitsRegex = leadingDigits.Length - MIN_LEADING_DIGITS_LENGTH;
            foreach (var format in possibleFormats.ToArray())
            {
                if (format.leadingDigitsPatternSize() == 0)
                {
                    // Keep everything that isn't restricted by leading digits.
                    continue;
                }
                int lastLeadingDigitsRegex =
                    Math.Min(indexOfLeadingDigitsRegex, format.leadingDigitsPatternSize() - 1);
                JavaRegex leadingDigitsRegex = regexCache.getRegexForRegex(
                    format.getLeadingDigitsPattern(lastLeadingDigitsRegex));
                var m = leadingDigitsRegex.MatchBeginning(leadingDigits);
                if (!m.Success)
                {
                    possibleFormats.Remove(format);
                }
            }
        }

        private bool createFormattingTemplate(NumberFormat format)
        {
            String numberRegex = format.getPattern();

            // The formatter doesn't format numbers when numberRegex contains "|", e.g.
            // (20|3)\d{4}. In those cases we quickly return.
            if (numberRegex.IndexOf('|') != -1)
            {
                return false;
            }

            // Replace anything in the form of [..] with \d
            numberRegex = CHARACTER_CLASS_PATTERN.Replace(numberRegex, "\\d");

            // Replace any standalone digit (not the one in d{}) with \d
            numberRegex = STANDALONE_DIGIT_PATTERN.Replace(numberRegex, "\\d");
            formattingTemplate.Clear();
            String tempTemplate = getFormattingTemplate(numberRegex, format.getFormat());
            if (tempTemplate.Length > 0)
            {
                formattingTemplate.Append(tempTemplate);
                return true;
            }
            return false;
        }

        // Gets a formatting template which can be used to efficiently format a partial number where
        // digits are added one by one.
        private String getFormattingTemplate(String numberPattern, String numberFormat)
        {
            // Creates a phone number consisting only of the digit 9 that matches the
            // numberRegex by applying the pattern to the longestPhoneNumber string.
            String longestPhoneNumber = "999999999999999";
            var m = regexCache.getRegexForRegex(numberPattern).Match(longestPhoneNumber);
//    m.find();  // this will always succeed
            String aPhoneNumber = m.Value;
            // No formatting template can be created if the number of digits entered so far is longer than
            // the maximum the current formatting rule can accommodate.
            if (aPhoneNumber.Length < nationalNumber.Length)
            {
                return "";
            }
            // Formats the number according to numberFormat
            String template = Regex.Replace(aPhoneNumber, numberPattern, numberFormat);
            // Replaces each digit with character DIGIT_PLACEHOLDER
            template = template.Replace("9", DIGIT_PLACEHOLDER);
            return template;
        }

        /**
   * Clears the internal state of the formatter, so it can be reused.
   */

        public void clear()
        {
            currentOutput = "";
            accruedInput.Clear();
            accruedInputWithoutFormatting.Clear();
            formattingTemplate.Clear();
            lastMatchPosition = 0;
            currentFormattingPattern = "";
            prefixBeforeNationalNumber.Clear();
            extractedNationalPrefix = "";
            nationalNumber.Clear();
            ableToFormat = true;
            inputHasFormatting = false;
            positionToRemember = 0;
            originalPosition = 0;
            isCompleteNumber = false;
            isExpectingCountryCallingCode = false;
            possibleFormats.Clear();
            shouldAddSpaceAfterNationalPrefix = false;
            if (!currentMetadata.Equals(defaultMetadata))
            {
                currentMetadata = getMetadataForRegion(defaultCountry);
            }
        }

        /**
   * Formats a phone number on-the-fly as each digit is entered.
   *
   * @param nextChar  the most recently entered digit of a phone number. Formatting characters are
   *     allowed, but as soon as they are encountered this method formats the number as entered and
   *     not "as you type" anymore. Full width digits and Arabic-indic digits are allowed, and will
   *     be shown as they are.
   * @return  the partially formatted phone number.
   */

        public String inputDigit(char nextChar)
        {
            currentOutput = inputDigitWithOptionToRememberPosition(nextChar, false);
            return currentOutput;
        }

        /**
   * Same as {@link #inputDigit}, but remembers the position where {@code nextChar} is inserted, so
   * that it can be retrieved later by using {@link #getRememberedPosition}. The remembered
   * position will be automatically adjusted if additional formatting characters are later
   * inserted/removed in front of {@code nextChar}.
   */

        public String inputDigitAndRememberPosition(char nextChar)
        {
            currentOutput = inputDigitWithOptionToRememberPosition(nextChar, true);
            return currentOutput;
        }

        private String inputDigitWithOptionToRememberPosition(char nextChar, bool rememberPosition)
        {
            accruedInput.Append(nextChar);
            if (rememberPosition)
            {
                originalPosition = accruedInput.Length;
            }
            // We do formatting on-the-fly only when each character entered is either a digit, or a plus
            // sign (accepted at the start of the number only).
            if (!isDigitOrLeadingPlusSign(nextChar))
            {
                ableToFormat = false;
                inputHasFormatting = true;
            }
            else
            {
                nextChar = normalizeAndAccrueDigitsAndPlusSign(nextChar, rememberPosition);
            }
            if (!ableToFormat)
            {
                // When we are unable to format because of reasons other than that formatting chars have been
                // entered, it can be due to really long IDDs or NDDs. If that is the case, we might be able
                // to do formatting again after extracting them.
                if (inputHasFormatting)
                {
                    return accruedInput.ToString();
                }
                else if (attemptToExtractIdd())
                {
                    if (attemptToExtractCountryCallingCode())
                    {
                        return attemptToChoosePatternWithPrefixExtracted();
                    }
                }
                else if (ableToExtractLongerNdd())
                {
                    // Add an additional space to separate long NDD and national significant number for
                    // readability. We don't set shouldAddSpaceAfterNationalPrefix to true, since we don't want
                    // this to change later when we choose formatting templates.
                    prefixBeforeNationalNumber.Append(SEPARATOR_BEFORE_NATIONAL_NUMBER);
                    return attemptToChoosePatternWithPrefixExtracted();
                }
                return accruedInput.ToString();
            }

            // We start to attempt to format only when at least MIN_LEADING_DIGITS_LENGTH digits (the plus
            // sign is counted as a digit as well for this purpose) have been entered.
            switch (accruedInputWithoutFormatting.Length)
            {
                case 0:
                case 1:
                case 2:
                    return accruedInput.ToString();
                case 3:
                    if (attemptToExtractIdd())
                    {
                        isExpectingCountryCallingCode = true;
                        goto default;
                    }
                    else
                    {
                        // No IDD or plus sign is found, might be entering in national format.
                        extractedNationalPrefix = removeNationalPrefixFromNationalNumber();
                        return attemptToChooseFormattingPattern();
                    }
                default:
                    if (isExpectingCountryCallingCode)
                    {
                        if (attemptToExtractCountryCallingCode())
                        {
                            isExpectingCountryCallingCode = false;
                        }
                        return prefixBeforeNationalNumber + nationalNumber.ToString();
                    }
                    if (possibleFormats.Count > 0)
                    {
                        // The formatting patterns are already chosen.
                        String tempNationalNumber = inputDigitHelper(nextChar);
                        // See if the accrued digits can be formatted properly already. If not, use the results
                        // from inputDigitHelper, which does formatting based on the formatting pattern chosen.
                        String formattedNumber = attemptToFormatAccruedDigits();
                        if (formattedNumber.Length > 0)
                        {
                            return formattedNumber;
                        }
                        narrowDownPossibleFormats(nationalNumber.ToString());
                        if (maybeCreateNewTemplate())
                        {
                            return inputAccruedNationalNumber();
                        }
                        return ableToFormat
                            ? appendNationalNumber(tempNationalNumber)
                            : accruedInput.ToString();
                    }
                    else
                    {
                        return attemptToChooseFormattingPattern();
                    }
            }
        }

        private String attemptToChoosePatternWithPrefixExtracted()
        {
            ableToFormat = true;
            isExpectingCountryCallingCode = false;
            possibleFormats.Clear();
            return attemptToChooseFormattingPattern();
        }

        // @VisibleForTesting
        internal String getExtractedNationalPrefix()
        {
            return extractedNationalPrefix;
        }

        // Some national prefixes are a substring of others. If extracting the shorter NDD doesn't result
        // in a number we can format, we try to see if we can extract a longer version here.
        private bool ableToExtractLongerNdd()
        {
            if (extractedNationalPrefix.Length > 0)
            {
                // Put the extracted NDD back to the national number before attempting to extract a new NDD.
                nationalNumber.Insert(0, extractedNationalPrefix);
                // Remove the previously extracted NDD from prefixBeforeNationalNumber. We cannot simply set
                // it to empty string because people sometimes incorrectly enter national prefix after the
                // country code, e.g. +44 (0)20-1234-5678.
                int indexOfPreviousNdd = prefixBeforeNationalNumber.LastIndexOf(extractedNationalPrefix);
                prefixBeforeNationalNumber.Length = indexOfPreviousNdd;
            }
            return !extractedNationalPrefix.Equals(removeNationalPrefixFromNationalNumber());
        }

        private bool isDigitOrLeadingPlusSign(char nextChar)
        {
            return char.IsDigit(nextChar) ||
                   (accruedInput.Length == 1 &&
                    PhoneNumberUtil.PLUS_CHARS_PATTERN.MatchWhole(nextChar.ToString()).Success);
        }

        /**
   * Check to see if there is an exact pattern match for these digits. If so, we should use this
   * instead of any other formatting template whose leadingDigitsRegex also matches the input.
   */

        private String attemptToFormatAccruedDigits()
        {
            foreach (NumberFormat numberFormat in possibleFormats)
            {
                var regex = regexCache.getRegexForRegex(numberFormat.getPattern());
                var m = regex.MatchWhole(nationalNumber);
                if (m.Success)
                {
                    shouldAddSpaceAfterNationalPrefix =
                        NATIONAL_PREFIX_SEPARATORS_PATTERN.Match(
                            numberFormat.getNationalPrefixFormattingRule()).Success;
                    String formattedNumber = regex.Replace(nationalNumber.ToString(), numberFormat.getFormat());
                    return appendNationalNumber(formattedNumber);
                }
            }
            return "";
        }

        /**
   * Returns the current position in the partially formatted phone number of the character which was
   * previously passed in as the parameter of {@link #inputDigitAndRememberPosition}.
   */

        public int getRememberedPosition()
        {
            if (!ableToFormat)
            {
                return originalPosition;
            }
            int accruedInputIndex = 0, currentOutputIndex = 0;
            while (accruedInputIndex < positionToRemember && currentOutputIndex < currentOutput.Length)
            {
                if (accruedInputWithoutFormatting[accruedInputIndex] ==
                    currentOutput[currentOutputIndex])
                {
                    accruedInputIndex++;
                }
                currentOutputIndex++;
            }
            return currentOutputIndex;
        }

        /**
   * Combines the national number with any prefix (IDD/+ and country code or national prefix) that
   * was collected. A space will be inserted between them if the current formatting template
   * indicates this to be suitable.
   */

        private String appendNationalNumber(String nationalNumber)
        {
            int prefixBeforeNationalNumberLength = prefixBeforeNationalNumber.Length;
            if (shouldAddSpaceAfterNationalPrefix && prefixBeforeNationalNumberLength > 0 &&
                prefixBeforeNationalNumber[prefixBeforeNationalNumberLength - 1]
                != SEPARATOR_BEFORE_NATIONAL_NUMBER)
            {
                // We want to add a space after the national prefix if the national prefix formatting rule
                // indicates that this would normally be done, with the exception of the case where we already
                // appended a space because the NDD was surprisingly long.
                return prefixBeforeNationalNumber.ToString() + SEPARATOR_BEFORE_NATIONAL_NUMBER
                       + nationalNumber;
            }
            else
            {
                return prefixBeforeNationalNumber + nationalNumber;
            }
        }

        /**
   * Attempts to set the formatting template and returns a string which contains the formatted
   * version of the digits entered so far.
   */

        private String attemptToChooseFormattingPattern()
        {
            // We start to attempt to format only when at least MIN_LEADING_DIGITS_LENGTH digits of national
            // number (excluding national prefix) have been entered.
            if (nationalNumber.Length >= MIN_LEADING_DIGITS_LENGTH)
            {

                getAvailableFormats(nationalNumber.ToString());
                // See if the accrued digits can be formatted properly already.
                String formattedNumber = attemptToFormatAccruedDigits();
                if (formattedNumber.Length > 0)
                {
                    return formattedNumber;
                }
                return maybeCreateNewTemplate() ? inputAccruedNationalNumber() : accruedInput.ToString();
            }
            else
            {
                return appendNationalNumber(nationalNumber.ToString());
            }
        }

        /**
   * Invokes inputDigitHelper on each digit of the national number accrued, and returns a formatted
   * string in the end.
   */

        private String inputAccruedNationalNumber()
        {
            int lengthOfNationalNumber = nationalNumber.Length;
            if (lengthOfNationalNumber > 0)
            {
                String tempNationalNumber = "";
                for (int i = 0; i < lengthOfNationalNumber; i++)
                {
                    tempNationalNumber = inputDigitHelper(nationalNumber[i]);
                }
                return ableToFormat ? appendNationalNumber(tempNationalNumber) : accruedInput.ToString();
            }
            else
            {
                return prefixBeforeNationalNumber.ToString();
            }
        }

        /**
   * Returns true if the current country is a NANPA country and the national number begins with
   * the national prefix.
   */

        private bool isNanpaNumberWithNationalPrefix()
        {
            // For NANPA numbers beginning with 1[2-9], treat the 1 as the national prefix. The reason is
            // that national significant numbers in NANPA always start with [2-9] after the national prefix.
            // Numbers beginning with 1[01] can only be short/emergency numbers, which don't need the
            // national prefix.
            return (currentMetadata.getCountryCode() == 1) && (nationalNumber[0] == '1') &&
                   (nationalNumber[1] != '0') && (nationalNumber[1] != '1');
        }

        // Returns the national prefix extracted, or an empty string if it is not present.
        private String removeNationalPrefixFromNationalNumber()
        {
            int startOfNationalNumber = 0;
            if (isNanpaNumberWithNationalPrefix())
            {
                startOfNationalNumber = 1;
                prefixBeforeNationalNumber.Append('1').Append(SEPARATOR_BEFORE_NATIONAL_NUMBER);
                isCompleteNumber = true;
            }
            else if (currentMetadata.HasNationalPrefixForParsing())
            {
                var nationalPrefixForParsing =
                    regexCache.getRegexForRegex(currentMetadata.getNationalPrefixForParsing());
                var m = nationalPrefixForParsing.MatchBeginning(nationalNumber);
                // Since some national prefix patterns are entirely optional, check that a national prefix
                // could actually be extracted.
                var end = m.Index + m.Length;
                if (m.Success && end > 0)
                {
                    // When the national prefix is detected, we use international formatting rules instead of
                    // national ones, because national formatting rules could contain local formatting rules
                    // for numbers entered without area code.
                    isCompleteNumber = true;
                    startOfNationalNumber = end;
                    prefixBeforeNationalNumber.Append(nationalNumber.Substring(0, startOfNationalNumber));
                }
            }
            String nationalPrefix = nationalNumber.Substring(0, startOfNationalNumber);
            nationalNumber.Delete(0, startOfNationalNumber);
            return nationalPrefix;
        }

        /**
   * Extracts IDD and plus sign to prefixBeforeNationalNumber when they are available, and places
   * the remaining input into nationalNumber.
   *
   * @return  true when accruedInputWithoutFormatting begins with the plus sign or valid IDD for
   *     defaultCountry.
   */

        private bool attemptToExtractIdd()
        {
            var internationalPrefix =
                regexCache.getRegexForRegex("\\" + PhoneNumberUtil.PLUS_SIGN + "|" +
                                            currentMetadata.getInternationalPrefix());
            var iddMatcher = internationalPrefix.MatchBeginning(accruedInputWithoutFormatting);
            if (iddMatcher.Success)
            {
                isCompleteNumber = true;
                int startOfCountryCallingCode = iddMatcher.Index + iddMatcher.Length;
                nationalNumber.Length = 0;
                nationalNumber.Append(accruedInputWithoutFormatting.Substring(startOfCountryCallingCode));
                prefixBeforeNationalNumber.Length = 0;
                prefixBeforeNationalNumber.Append(
                    accruedInputWithoutFormatting.Substring(0, startOfCountryCallingCode));
                if (accruedInputWithoutFormatting[0] != PhoneNumberUtil.PLUS_SIGN)
                {
                    prefixBeforeNationalNumber.Append(SEPARATOR_BEFORE_NATIONAL_NUMBER);
                }
                return true;
            }
            return false;
        }

        /**
   * Extracts the country calling code from the beginning of nationalNumber to
   * prefixBeforeNationalNumber when they are available, and places the remaining input into
   * nationalNumber.
   *
   * @return  true when a valid country calling code can be found.
   */

        private bool attemptToExtractCountryCallingCode()
        {
            if (nationalNumber.Length == 0)
            {
                return false;
            }
            StringBuilder numberWithoutCountryCallingCode = new StringBuilder();
            int countryCode = phoneUtil.extractCountryCode(nationalNumber, numberWithoutCountryCallingCode);
            if (countryCode == 0)
            {
                return false;
            }
            nationalNumber.Length = 0;
            nationalNumber.Append(numberWithoutCountryCallingCode);
            String newRegionCode = phoneUtil.getRegionCodeForCountryCode(countryCode);
            if (PhoneNumberUtil.REGION_CODE_FOR_NON_GEO_ENTITY.Equals(newRegionCode))
            {
                currentMetadata = phoneUtil.getMetadataForNonGeographicalRegion(countryCode);
            }
            else if (!newRegionCode.Equals(defaultCountry))
            {
                currentMetadata = getMetadataForRegion(newRegionCode);
            }
            String countryCodeString = countryCode.ToString();
            prefixBeforeNationalNumber.Append(countryCodeString).Append(SEPARATOR_BEFORE_NATIONAL_NUMBER);
            // When we have successfully extracted the IDD, the previously extracted NDD should be cleared
            // because it is no longer valid.
            extractedNationalPrefix = "";
            return true;
        }

        // Accrues digits and the plus sign to accruedInputWithoutFormatting for later use. If nextChar
        // contains a digit in non-ASCII format (e.g. the full-width version of digits), it is first
        // normalized to the ASCII version. The return value is nextChar itself, or its normalized
        // version, if nextChar is a digit in non-ASCII format. This method assumes its input is either a
        // digit or the plus sign.
        private char normalizeAndAccrueDigitsAndPlusSign(char nextChar, bool rememberPosition)
        {
            char normalizedChar;
            if (nextChar == PhoneNumberUtil.PLUS_SIGN)
            {
                normalizedChar = nextChar;
                accruedInputWithoutFormatting.Append(nextChar);
            }
            else
            {
                normalizedChar = ((int) char.GetNumericValue(nextChar)).ToString()[0];
                accruedInputWithoutFormatting.Append(normalizedChar);
                nationalNumber.Append(normalizedChar);
            }
            if (rememberPosition)
            {
                positionToRemember = accruedInputWithoutFormatting.Length;
            }
            return normalizedChar;
        }

        private String inputDigitHelper(char nextChar)
        {
            var input = formattingTemplate.ToString();
            var digitMatcher = DIGIT_PATTERN.Match(input, lastMatchPosition);
            if (digitMatcher.Success)
            {
                String tempTemplate = DIGIT_PATTERN.Replace(input, nextChar.ToString(), 1, lastMatchPosition);
                formattingTemplate.Replace(0, tempTemplate.Length, tempTemplate);
                lastMatchPosition = digitMatcher.Index;
                return formattingTemplate.Substring(0, lastMatchPosition + 1);
            }
            else
            {
                if (possibleFormats.Count == 1)
                {
                    // More digits are entered than we could handle, and there are no other valid patterns to
                    // try.
                    ableToFormat = false;
                } // else, we just reset the formatting pattern.
                currentFormattingPattern = "";
                return accruedInput.ToString();
            }
        }
    }
}
