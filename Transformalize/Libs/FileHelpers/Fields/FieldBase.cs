#region License

// /*
// Transformalize - Replicate, Transform, and Denormalize Your Data...
// Copyright (C) 2013 Dale Newman
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// */

#endregion

using System;
using System.Reflection;
using System.Text;
using Transformalize.Libs.FileHelpers.Attributes;
using Transformalize.Libs.FileHelpers.Converters;
using Transformalize.Libs.FileHelpers.Core;
using Transformalize.Libs.FileHelpers.Enums;
using Transformalize.Libs.FileHelpers.ErrorHandling;
using Transformalize.Libs.FileHelpers.Helpers;
using Transformalize.Libs.NLog;

namespace Transformalize.Libs.FileHelpers.Fields
{
    /// <summary>Base class for all Field Types. Implements all the basic functionality of a field in a typed file.</summary>
    internal abstract class FieldBase
    {
        #region "  Private & Internal Fields  "

        private readonly Logger _log = LogManager.GetCurrentClassLogger();
        private static readonly Type strType = typeof (string);

        internal Type mFieldType;
        internal bool mIsStringField;
        internal FieldInfo mFieldInfo;

        internal TrimMode mTrimMode = TrimMode.None;
        internal Char[] mTrimChars = null;
        internal bool mIsOptional = false;
        internal bool mNextIsOptional = false;
        internal bool mInNewLine = false;

        internal bool mIsFirst = false;
        internal bool mIsLast = false;
        internal bool mTrailingArray = false;


        internal object mNullValue = null;
        //internal bool mNullValueOnWrite = false;

#if NET_2_0
		private bool mIsNullableType = false;
#endif

        #endregion

        #region "  Constructor  " 

        protected FieldBase(FieldInfo fi)
        {
            mFieldInfo = fi;
            mFieldType = mFieldInfo.FieldType;
            mIsStringField = mFieldType == strType;

            var attribs = fi.GetCustomAttributes(typeof (FieldConverterAttribute), true);

            if (attribs.Length > 0)
            {
                var conv = (FieldConverterAttribute) attribs[0];
                mConvertProvider = conv.Converter;
                conv.ValidateTypes(mFieldInfo);
            }
            else
                mConvertProvider = ConvertHelpers.GetDefaultConverter(fi.Name, mFieldType);

            if (mConvertProvider != null)
                mConvertProvider.mDestinationType = fi.FieldType;

            attribs = fi.GetCustomAttributes(typeof (FieldNullValueAttribute), true);

            if (attribs.Length > 0)
            {
                mNullValue = ((FieldNullValueAttribute) attribs[0]).NullValue;
//				mNullValueOnWrite = ((FieldNullValueAttribute) attribs[0]).NullValueOnWrite;

                if (mNullValue != null)
                {
                    if (! mFieldType.IsAssignableFrom(mNullValue.GetType()))
                        throw new BadUsageException("The NullValue is of type: " + mNullValue.GetType().Name +
                                                    " that is not asignable to the field " + mFieldInfo.Name + " of type: " +
                                                    mFieldType.Name);
                }
            }


#if NET_2_0
			mIsNullableType = mFieldType.IsValueType &&
									mFieldType.IsGenericType && 
									mFieldType.GetGenericTypeDefinition() == typeof(Nullable<>);
#endif
        }

        #endregion

        private static readonly char[] WhitespaceChars = new[]
                                                             {
                                                                 '\t', '\n', '\v', '\f', '\r', ' ', '\x00a0', '\u2000', '\u2001', '\u2002', '\u2003', '\u2004', '\u2005', '\u2006', '\u2007', '\u2008',
                                                                 '\u2009', '\u200a', '\u200b', '\u3000', '\ufeff'
                                                             };

        #region "  MustOverride (String Handling)  " 

        protected abstract ExtractedInfo ExtractFieldString(LineInfo line);

        protected abstract void CreateFieldString(StringBuilder sb, object fieldValue);

        protected string BaseFieldString(object fieldValue)
        {
            if (mConvertProvider == null)
            {
                if (fieldValue == null)
                    return string.Empty;
                else
                    return fieldValue.ToString();

//				else if (mNullValueOnWrite && fieldValue.Equals(mNullValue))
//					res = string.Empty;
            }
            else
            {
                return mConvertProvider.FieldToString(fieldValue);
            }
        }

        protected int mCharsToDiscard = 0;

        #endregion

        internal ConverterBase mConvertProvider;

        #region "  ExtractValue  " 

// object[] values, int index, ForwardReader reader
        internal object ExtractValue(LineInfo line)
        {
            //-> extract only what I need

            if (mInNewLine)
            {
                if (line.EmptyFromPos() == false)
                    throw new BadUsageException("Text '" + line.CurrentString +
                                                "' found before the new line of the field: " + mFieldInfo.Name +
                                                " (this is not allowed when you use [FieldInNewLine])");

                line.ReLoad(line.mReader.ReadNextLine());

                if (line.mLineStr == null)
                    throw new BadUsageException("End of stream found parsing the field " + mFieldInfo.Name +
                                                ". Please check the class record.");
            }

            var info = ExtractFieldString(line);
            if (info.mCustomExtractedString == null)
                line.mCurrentPos = info.ExtractedTo + 1;

            line.mCurrentPos += mCharsToDiscard; //total;

            return AssignFromString(info, line);


            //-> discard the part that I use


            //TODO: Uncoment this for Quoted Handling
//			if (info.NewRestOfLine != null)
//			{
//				if (info.NewRestOfLine.Length < CharsToDiscard())
//					return info.NewRestOfLine;
//				else
//					return info.NewRestOfLine.Substring(CharsToDiscard());
//			}
//			else
//			{
//				int total;
//				if (info.CharsRemoved >= line.mLine.Length)
//					total = line.mLine.Length;
//				else
//					total = info.CharsRemoved + CharsToDiscard();

            //return buffer.Substring(total);
//			}
        }

        #region "  AssignFromString  " 

        internal object AssignFromString(ExtractedInfo fieldString, LineInfo line)
        {
            object val;

            switch (mTrimMode)
            {
                case TrimMode.None:
                    break;

                case TrimMode.Both:
                    fieldString.TrimBoth(mTrimChars);
                    break;

                case TrimMode.Left:
                    fieldString.TrimStart(mTrimChars);
                    break;

                case TrimMode.Right:
                    fieldString.TrimEnd(mTrimChars);
                    break;
            }

            try
            {
                if (mConvertProvider == null)
                {
                    if (mIsStringField)
                        val = fieldString.ExtractedString();
                    else
                    {
                        // Trim it to use Convert.ChangeType
                        fieldString.TrimBoth(WhitespaceChars);

                        if (fieldString.Length == 0)
                        {
                            // Empty stand for null
                            val = GetNullValue();
                        }
                        else
                        {
                            val = Convert.ChangeType(fieldString.ExtractedString(), mFieldType, null);
                        }
                    }
                }

                else
                {
                    if (mConvertProvider.CustomNullHandling == false &&
                        fieldString.HasOnlyThisChars(WhitespaceChars))
                    {
                        val = GetNullValue();
                    }
                    else
                    {
                        var from = fieldString.ExtractedString();
                        val = mConvertProvider.StringToField(from);

                        if (val == null)
                            val = GetNullValue();
                    }
                }

                return val;
            }
            catch (ConvertException ex)
            {
                var e = ConvertException.ReThrowException(ex, mFieldInfo.Name, line.mReader.LineNumber, fieldString.ExtractedFrom + 1);
                _log.Error("{0} {1}", e.Message, e.MessageExtra);
                Environment.Exit(1);
            }
            return null;
        }

        private object GetNullValue()
        {
            if (mNullValue == null)
            {
                if (mFieldType.IsValueType)
                {
#if NET_2_0
				   if ( mIsNullableType )
						return null;
						
					throw new BadUsageException("Null Value found for the field '" + mFieldInfo.Name + "' in the class '" +
						mFieldInfo.DeclaringType.Name +
						"'. You must specify a FieldNullValue attribute because this is a ValueType and can�t be null or you can use the Nullable Types feature of the .NET framework.");
#else
                    throw new BadUsageException("Null Value found for the field '" + mFieldInfo.Name + "' in the class '" +
                                                mFieldInfo.DeclaringType.Name +
                                                "'. You must specify a FieldNullValueAttribute because this is a ValueType and can�t be null.");

#endif
                }
                else
                    return null;
            }
            else
                return mNullValue;
        }

        #endregion

        #region "  CreateValueForField  " 

        public object CreateValueForField(object fieldValue)
        {
            object val = null;

            if (fieldValue == null)
            {
                if (mNullValue == null)
                {
                    if (mFieldType.IsValueType)
                        throw new BadUsageException("Null Value found. You must specify a NullValueAttribute in the " + mFieldInfo.Name +
                                                    " field of type " + mFieldInfo.FieldType.Name + ", because this is a ValueType.");
                    else
                        val = null;
                }
                else
                {
                    val = mNullValue;
                }
            }
            else if (mFieldType == fieldValue)
                val = fieldValue;
            else
            {
                if (mConvertProvider == null)
                    val = Convert.ChangeType(fieldValue, mFieldType, null);
                else
                {
                    try
                    {
                        val = Convert.ChangeType(fieldValue, mFieldType, null);
                    }
                    catch
                    {
                        val = mConvertProvider.StringToField(fieldValue.ToString());
                    }
                }
            }

            return val;
        }

        #endregion

        #endregion

        #region "  AssignToString  " 

        internal void AssignToString(StringBuilder sb, object fieldValue)
        {
            if (mInNewLine)
                sb.Append(StringHelper.NewLine);

            CreateFieldString(sb, fieldValue);
        }

        #endregion
    }
}