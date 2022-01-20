
// **********************************************************************************************************
// This is an auto generated file and any changes directly applied to this file will be lost in next generation.
// Please DO NOT modify this file but instead, update .+LogMessage\.json files and/or LogCode.tt.
// **********************************************************************************************************
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ECMA2Yaml
{
    public class LogCode : LogCodeBase
    {
    #region ECMA2Yaml
		public static readonly LogCode ECMA2Yaml_Info = new LogCode("ECMA2Yaml_Info", "This log level is only used to write information.");
		public static readonly LogCode ECMA2Yaml_SDP_MigrationNeeded = new LogCode("ECMA2Yaml_SDP_MigrationNeeded", "This repo/docset is not SDP-enabled. Please contact apidocs-team@service.microsoft.com to enable SDP on this repo.");
		public static readonly LogCode ECMA2Yaml_Uid_Duplicated = new LogCode("ECMA2Yaml_Uid_Duplicated", "Duplicate uid found: {0}");
		public static readonly LogCode ECMA2Yaml_DocId_Duplicated = new LogCode("ECMA2Yaml_DocId_Duplicated", "Duplicated DocId found: {0}");
		public static readonly LogCode ECMA2Yaml_MemberGroup_Duplicated = new LogCode("ECMA2Yaml_MemberGroup_Duplicated", "Found duplicated <MemberGroup> {0}");
		public static readonly LogCode ECMA2Yaml_DocId_IsNull = new LogCode("ECMA2Yaml_DocId_IsNull", "DocId is required for {0}.");
		public static readonly LogCode ECMA2Yaml_MemberNameAndSignature_NotUnique = new LogCode("ECMA2Yaml_MemberNameAndSignature_NotUnique", "Member {0}'s name and signature is not unique");
		public static readonly LogCode ECMA2Yaml_Framework_NotFound = new LogCode("ECMA2Yaml_Framework_NotFound", "Unable to find framework info for {0}");
		public static readonly LogCode ECMA2Yaml_Type_NotFound = new LogCode("ECMA2Yaml_Type_NotFound", "Unable to identify the type of Type {0}");
		public static readonly LogCode ECMA2Yaml_TypeString_ParseFailed = new LogCode("ECMA2Yaml_TypeString_ParseFailed", "Unable to parse type string {0}");
		public static readonly LogCode ECMA2Yaml_CommentID_ParseFailed = new LogCode("ECMA2Yaml_CommentID_ParseFailed", "Unable to parse string as comment id: {0}");
		public static readonly LogCode ECMA2Yaml_PackageInformation_LoadFailed = new LogCode("ECMA2Yaml_PackageInformation_LoadFailed", "Unable to load package information: {0}");
		public static readonly LogCode ECMA2Yaml_MonikerToAssembly_Failed = new LogCode("ECMA2Yaml_MonikerToAssembly_Failed", "Unable to load moniker to assembly mapping: {0}");
		public static readonly LogCode ECMA2Yaml_Moniker_EmptyAssembly = new LogCode("ECMA2Yaml_Moniker_EmptyAssembly", "{0} have empty assembly.");
		public static readonly LogCode ECMA2Yaml_CommentId_ResolveFailed = new LogCode("ECMA2Yaml_CommentId_ResolveFailed", "Unable to resolve: <InterfaceMember>{0}</InterfaceMember>");
		public static readonly LogCode ECMA2Yaml_Namespace_LoadFailed = new LogCode("ECMA2Yaml_Namespace_LoadFailed", "Failed to load namespace");
		public static readonly LogCode ECMA2Yaml_File_LoadFailed = new LogCode("ECMA2Yaml_File_LoadFailed", "Failed to load {0} files, aborting...");
		public static readonly LogCode ECMA2Yaml_Uid_NotFound = new LogCode("ECMA2Yaml_Uid_NotFound", "Can't find uid in yaml header: {0}");
		public static readonly LogCode ECMA2Yaml_NotesType_UnKnown = new LogCode("ECMA2Yaml_NotesType_UnKnown", "Can't recognize additional notes type: {0}");
		public static readonly LogCode ECMA2Yaml_UidAssembly_NotMatched = new LogCode("ECMA2Yaml_UidAssembly_NotMatched", "{0}'s moniker {1} can't match any assembly.");
		public static readonly LogCode ECMA2Yaml_ExtraMonikerFoundInMember = new LogCode("ECMA2Yaml_ExtraMonikerFoundInMember", "Moniker {0} exists in member {1} but can't be found in parent type.");
		public static readonly LogCode ECMA2Yaml_Command_Invalid = new LogCode("ECMA2Yaml_Command_Invalid", "Invalid command line parameter.");
		public static readonly LogCode ECMA2Yaml_InternalError = new LogCode("ECMA2Yaml_InternalError", "Intenal Several Error: {0}");
		public static readonly LogCode ECMA2Yaml_Namespace_NoTypes = new LogCode("ECMA2Yaml_Namespace_NoTypes", "Namespace {0} has no types");
		public static readonly LogCode ECMA2Yaml_Type_ExternalBaseType = new LogCode("ECMA2Yaml_Type_ExternalBaseType", "Type {0} has an external base type {1}");
		public static readonly LogCode ECMA2Yaml_ExceptionTypeNotFound = new LogCode("ECMA2Yaml_ExceptionTypeNotFound", "Referenced exception type not found: {0}");
		public static readonly LogCode ECMA2Yaml_CrefTypePrefixMissing = new LogCode("ECMA2Yaml_CrefTypePrefixMissing", "Invalid cref format ({0}) detected in {1}");
		public static readonly LogCode ECMA2Yaml_Member_EmptyMoniker = new LogCode("ECMA2Yaml_Member_EmptyMoniker", "{0} have empty Moniker");
		public static readonly LogCode ECMA2Yaml_Enum_NoRemarks = new LogCode("ECMA2Yaml_Enum_NoRemarks", "Please note: <remarks> node on Enum fields will be ignored.");
		public static readonly LogCode ECMA2Yaml_Inheritdoc_NoFoundParent = new LogCode("ECMA2Yaml_Inheritdoc_NoFoundParent", "Found no member can be inherited by key:{0} for uid: {1}.");
		public static readonly LogCode ECMA2Yaml_Inheritdoc_NoFoundDocs = new LogCode("ECMA2Yaml_Inheritdoc_NoFoundDocs", "Inheridoc tag exists but no inheritdoc found for uid:{0}.");
		public static readonly LogCode ECMA2Yaml_Inheritdoc_InvalidTags = new LogCode("ECMA2Yaml_Inheritdoc_InvalidTags", "Inheridoc and summary tags both exists for uid:{0}.");
		public static readonly LogCode ECMA2Yaml_Inheritdoc_InvalidTagsForStatic = new LogCode("ECMA2Yaml_Inheritdoc_InvalidTagsForStatic", "Inheridoc should not use on static object for uid:{0}.");
		public static readonly LogCode ECMA2Yaml_Inheritdoc_NotSupportType = new LogCode("ECMA2Yaml_Inheritdoc_NotSupportType", "Inheridoc not support type: {0} for uid:{1}.");
		#endregion
    public LogCode(string code, string msgTemplate)
        : base(code, msgTemplate)
    {
    }
    }

	public abstract class LogCodeBase
    {
        public string Code { get; private set; }

        public string MessageTemplate { get; private set; }

        protected LogCodeBase(string code, string msgTemplate)
        {
            Code = code;
            MessageTemplate = msgTemplate;
        }

        public override string ToString() => Code;

        public static IEnumerable<T> GetAll<T>() where T : LogCodeBase
        {
            var fields = typeof(T).GetFields(BindingFlags.Public |
                                             BindingFlags.Static |
                                             BindingFlags.DeclaredOnly);

            return fields.Select(f => f.GetValue(null)).Cast<T>();
        }
    }
}