using System;
using System.Collections.Generic;
using System.Xml;

using Mono.Cecil;

using Mono.Documentation.Util;

namespace Mono.Documentation.Updater
{
    public class DocsNodeInfo
    {
        public DocsNodeInfo (XmlElement node)
        {
            this.Node = node;
        }

        public DocsNodeInfo (XmlElement node, TypeDefinition type)
            : this (node)
        {
            SetType (type);
        }

        public DocsNodeInfo (XmlElement node, MemberReference member)
            : this (node)
        {
            SetMemberInfo (member);
        }

        void SetType (TypeDefinition type)
        {
            if (type == null)
                throw new ArgumentNullException ("type");
            Type = type;
            GenericParameters = DocUtils.GetGenericParameters(type);

            if (DocUtils.IsDelegate (type))
            {
                Parameters = type.GetMethod ("Invoke").Parameters;
                ReturnType = type.GetMethod ("Invoke").ReturnType;
                ReturnIsReturn = true;
            }
        }

        void SetMemberInfo (MemberReference member)
        {
            if (member == null)
                throw new ArgumentNullException ("member");
            ReturnIsReturn = true;
            AddRemarks = true;
            Member = member;

            if (member is MethodReference)
            {
                MethodReference mr = (MethodReference)member;
                Parameters = mr.Parameters;
                if (mr.IsGenericMethod ())
                {
                    GenericParameters = new List<GenericParameter> (mr.GenericParameters);
                }
            }
            else if (member is PropertyDefinition)
            {
                Parameters = ((PropertyDefinition)member).Parameters;
            }

            if (member is MethodDefinition)
            {
                ReturnType = ((MethodDefinition)member).ReturnType;
            }
            else if (member is PropertyDefinition)
            {
                ReturnType = ((PropertyDefinition)member).PropertyType;
                ReturnIsReturn = false;
            }

            // no remarks section for enum members
            if (member.DeclaringType != null && ((TypeDefinition)member.DeclaringType).IsEnum)
                AddRemarks = false;
        }

        public TypeReference ReturnType;
        public List<GenericParameter> GenericParameters;
        public IList<ParameterDefinition> Parameters;
        public bool ReturnIsReturn;
        public XmlElement Node;
        public bool AddRemarks = true;
        public MemberReference Member;
        public TypeDefinition Type;

        public override string ToString ()
        {
            return string.Format ("{0} - {1} - {2}", Type, Member, Node == null ? "no xml" : "with xml");
        }
    }
}