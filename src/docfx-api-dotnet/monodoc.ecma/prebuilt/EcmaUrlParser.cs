// created by jay 0.7 (c) 1998 Axel.Schreiner@informatik.uni-osnabrueck.de

#line 1 "Monodoc.Ecma/EcmaUrlParser.jay"


// To Generate this file: 
// 1. Download and install mono-jay via https://packages.ubuntu.com/bionic/mono-jay
// 2. Run command: 
//    jay -ct Monodoc.Ecma/EcmaUrlParser.jay < Monodoc.Ecma/jay/skeleton.cs > Monodoc.Ecma/prebuilt/EcmaUrlParser.cs

using System.Text;
using System.IO;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Monodoc.Ecma
{
	public class EcmaUrlParser
	{
        int yacc_verbose_flag = 0;

        public void IsValid (string input)
        {
			var lexer = new EcmaUrlTokenizer (input);
			this.yyparse (lexer);
        }

        public EcmaDesc Parse (string input)
        {
			var lexer = new EcmaUrlTokenizer (input);
			return (EcmaDesc)this.yyparse (lexer);
        }

        public bool TryParse (string input, out EcmaDesc desc)
        {
            desc = null;
            try {
                desc = Parse (input);
            } catch {
                return false;
            }
            return true;
        }

        EcmaDesc SetEcmaDescType (object result, EcmaDesc.Kind kind)
        {
            var desc = result as EcmaDesc;
            desc.DescKind = kind;
            return desc;
        }

        List<T> SafeReverse<T> (List<T> input)
        {
            if (input == null)
               return null;
            input.Reverse ();
            return input;
        }
#line default

  /** error output stream.
      It should be changeable.
    */
  public System.IO.TextWriter ErrorOutput = System.Console.Out;

  /** simplified error message.
      @see <a href="#yyerror(java.lang.String, java.lang.String[])">yyerror</a>
    */
  public void yyerror (string message) {
    yyerror(message, null);
  }
#pragma warning disable 649
  /* An EOF token */
  public int eof_token;
#pragma warning restore 649
  /** (syntax) error message.
      Can be overwritten to control message format.
      @param message text to be displayed.
      @param expected vector of acceptable tokens, if available.
    */
  public void yyerror (string message, string[] expected) {
    if ((yacc_verbose_flag > 0) && (expected != null) && (expected.Length  > 0)) {
      ErrorOutput.Write (message+", expecting");
      for (int n = 0; n < expected.Length; ++ n)
        ErrorOutput.Write (" "+expected[n]);
        ErrorOutput.WriteLine ();
    } else
      ErrorOutput.WriteLine (message);
  }

  /** debugging support, requires the package jay.yydebug.
      Set to null to suppress debugging messages.
    */
  internal yydebug.yyDebug debug;

  protected const int yyFinal = 9;
 // Put this array into a separate class so it is only initialized if debugging is actually used
 // Use MarshalByRefObject to disable inlining
 class YYRules : MarshalByRefObject {
  public static readonly string [] yyRule = {
    "$accept : expression",
    "expression : 'T' COLON type_expression",
    "expression : 'N' COLON namespace_expression",
    "expression : 'M' COLON method_expression",
    "expression : 'F' COLON simple_member_expression",
    "expression : 'C' COLON constructor_expression",
    "expression : 'P' COLON property_expression",
    "expression : 'E' COLON simple_member_expression",
    "expression : 'O' COLON operator_expression",
    "dot_expression : IDENTIFIER",
    "dot_expression : IDENTIFIER DOT dot_expression",
    "namespace_expression : dot_expression",
    "type_expression : dot_expression type_expression_suffix",
    "reduced_type_expression : IDENTIFIER type_expression_suffix",
    "type_expression_suffix : opt_generic_type_suffix opt_inner_type_description opt_nullable opt_array_definition opt_nullable opt_etc",
    "opt_inner_type_description :",
    "opt_inner_type_description : INNER_TYPE_SEPARATOR reduced_type_expression",
    "opt_generic_type_suffix :",
    "opt_generic_type_suffix : OP_GENERICS_BACKTICK DIGIT",
    "opt_generic_type_suffix : OP_GENERICS_LT generic_type_arg_list OP_GENERICS_GT",
    "generic_type_arg_list : type_expression",
    "generic_type_arg_list : generic_type_arg_list COMMA type_expression",
    "opt_nullable :",
    "opt_nullable : QUESTION_MARK",
    "opt_array_definition :",
    "opt_array_definition : OP_ARRAY_OPEN opt_array_definition_list OP_ARRAY_CLOSE opt_array_definition",
    "opt_array_definition_list :",
    "opt_array_definition_list : COMMA opt_array_definition_list",
    "opt_etc :",
    "opt_etc : SLASH_SEPARATOR etc_identifier",
    "opt_etc : SLASH_SEPARATOR etc_identifier SLASH_SEPARATOR reduced_member_expression",
    "etc_identifier : STAR",
    "etc_identifier : IDENTIFIER",
    "method_expression : type_expression DOT IDENTIFIER opt_generic_type_suffix opt_arg_list_suffix",
    "method_expression : dot_expression opt_generic_type_suffix opt_arg_list_suffix",
    "method_expression : type_expression EXPLICIT_IMPL_SEP method_expression",
    "reduced_member_expression : IDENTIFIER opt_generic_type_suffix",
    "reduced_member_expression : IDENTIFIER opt_generic_type_suffix DOT reduced_member_expression",
    "arg_type_expression : type_expression opt_arg_type_suffix",
    "opt_arg_type_suffix :",
    "opt_arg_type_suffix : STAR",
    "opt_arg_type_suffix : REF_ARG",
    "opt_arg_type_suffix : OUT_ARG",
    "type_expression_list :",
    "type_expression_list : arg_type_expression",
    "type_expression_list : arg_type_expression COMMA type_expression_list",
    "simple_member_expression : dot_expression",
    "simple_member_expression : type_expression DOT IDENTIFIER",
    "simple_member_expression : type_expression EXPLICIT_IMPL_SEP simple_member_expression",
    "constructor_expression : method_expression",
    "operator_expression : method_expression",
    "property_expression : simple_member_expression opt_property_indexer",
    "opt_property_indexer : opt_arg_list_suffix",
    "opt_arg_list_suffix :",
    "opt_arg_list_suffix : OP_OPEN_PAREN type_expression_list OP_CLOSE_PAREN",
  };
 public static string getRule (int index) {
    return yyRule [index];
 }
}
  protected static readonly string [] yyNames = {    
    "end-of-file",null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,
    "'C'",null,"'E'","'F'",null,null,null,null,null,null,"'M'","'N'",
    "'O'","'P'",null,null,null,"'T'",null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,"ERROR",
    "IDENTIFIER","DIGIT","DOT","COMMA","COLON","INNER_TYPE_SEPARATOR",
    "OP_GENERICS_LT","OP_GENERICS_GT","OP_GENERICS_BACKTICK",
    "OP_OPEN_PAREN","OP_CLOSE_PAREN","OP_ARRAY_OPEN","OP_ARRAY_CLOSE",
    "SLASH_SEPARATOR","STAR","REF_ARG","OUT_ARG","EXPLICIT_IMPL_SEP",
    "QUESTION_MARK",
  };

  /** index-checked interface to yyNames[].
      @param token single character or %token value.
      @return token name or [illegal] or [unknown].
    */
  public static string yyname (int token) {
    if ((token < 0) || (token > yyNames.Length)) return "[illegal]";
    string name;
    if ((name = yyNames[token]) != null) return name;
    return "[unknown]";
  }

#pragma warning disable 414
  int yyExpectingState;
#pragma warning restore 414
  /** computes list of expected tokens on error by tracing the tables.
      @param state for which to compute the list.
      @return list of token names.
    */
  protected int [] yyExpectingTokens (int state){
    int token, n, len = 0;
    bool[] ok = new bool[yyNames.Length];
    if ((n = yySindex[state]) != 0)
      for (token = n < 0 ? -n : 0;
           (token < yyNames.Length) && (n+token < yyTable.Length); ++ token)
        if (yyCheck[n+token] == token && !ok[token] && yyNames[token] != null) {
          ++ len;
          ok[token] = true;
        }
    if ((n = yyRindex[state]) != 0)
      for (token = n < 0 ? -n : 0;
           (token < yyNames.Length) && (n+token < yyTable.Length); ++ token)
        if (yyCheck[n+token] == token && !ok[token] && yyNames[token] != null) {
          ++ len;
          ok[token] = true;
        }
    int [] result = new int [len];
    for (n = token = 0; n < len;  ++ token)
      if (ok[token]) result[n++] = token;
    return result;
  }
  protected string[] yyExpecting (int state) {
    int [] tokens = yyExpectingTokens (state);
    string [] result = new string[tokens.Length];
    for (int n = 0; n < tokens.Length;  n++)
      result[n++] = yyNames[tokens [n]];
    return result;
  }

  /** the generated parser, with debugging messages.
      Maintains a state and a value stack, currently with fixed maximum size.
      @param yyLex scanner.
      @param yydebug debug message writer implementing yyDebug, or null.
      @return result of the last reduction, if any.
      @throws yyException on irrecoverable parse error.
    */
  internal Object yyparse (yyParser.yyInput yyLex, Object yyd)
				 {
    this.debug = (yydebug.yyDebug)yyd;
    return yyparse(yyLex);
  }

  /** initial size and increment of the state/value stack [default 256].
      This is not final so that it can be overwritten outside of invocations
      of yyparse().
    */
  protected int yyMax;

  /** executed at the beginning of a reduce action.
      Used as $$ = yyDefault($1), prior to the user-specified action, if any.
      Can be overwritten to provide deep copy, etc.
      @param first value for $1, or null.
      @return first.
    */
  protected Object yyDefault (Object first) {
    return first;
  }

	static int[] global_yyStates;
	static object[] global_yyVals;
#pragma warning disable 649
	protected bool use_global_stacks;
#pragma warning restore 649
	object[] yyVals;					// value stack
	object yyVal;						// value stack ptr
	int yyToken;						// current input
	int yyTop;

  /** the generated parser.
      Maintains a state and a value stack, currently with fixed maximum size.
      @param yyLex scanner.
      @return result of the last reduction, if any.
      @throws yyException on irrecoverable parse error.
    */
  internal Object yyparse (yyParser.yyInput yyLex)
  {
    if (yyMax <= 0) yyMax = 256;		// initial size
    int yyState = 0;                   // state stack ptr
    int [] yyStates;               	// state stack 
    yyVal = null;
    yyToken = -1;
    int yyErrorFlag = 0;				// #tks to shift
	if (use_global_stacks && global_yyStates != null) {
		yyVals = global_yyVals;
		yyStates = global_yyStates;
   } else {
		yyVals = new object [yyMax];
		yyStates = new int [yyMax];
		if (use_global_stacks) {
			global_yyVals = yyVals;
			global_yyStates = yyStates;
		}
	}

    /*yyLoop:*/ for (yyTop = 0;; ++ yyTop) {
      if (yyTop >= yyStates.Length) {			// dynamically increase
        global::System.Array.Resize (ref yyStates, yyStates.Length+yyMax);
        global::System.Array.Resize (ref yyVals, yyVals.Length+yyMax);
      }
      yyStates[yyTop] = yyState;
      yyVals[yyTop] = yyVal;
      if (debug != null) debug.push(yyState, yyVal);

      /*yyDiscarded:*/ while (true) {	// discarding a token does not change stack
        int yyN;
        if ((yyN = yyDefRed[yyState]) == 0) {	// else [default] reduce (yyN)
          if (yyToken < 0) {
            yyToken = yyLex.advance() ? yyLex.token() : 0;
            if (debug != null)
              debug.lex(yyState, yyToken, yyname(yyToken), yyLex.value());
          }
          if ((yyN = yySindex[yyState]) != 0 && ((yyN += yyToken) >= 0)
              && (yyN < yyTable.Length) && (yyCheck[yyN] == yyToken)) {
            if (debug != null)
              debug.shift(yyState, yyTable[yyN], yyErrorFlag-1);
            yyState = yyTable[yyN];		// shift to yyN
            yyVal = yyLex.value();
            yyToken = -1;
            if (yyErrorFlag > 0) -- yyErrorFlag;
            goto continue_yyLoop;
          }
          if ((yyN = yyRindex[yyState]) != 0 && (yyN += yyToken) >= 0
              && yyN < yyTable.Length && yyCheck[yyN] == yyToken)
            yyN = yyTable[yyN];			// reduce (yyN)
          else
            switch (yyErrorFlag) {
  
            case 0:
              yyExpectingState = yyState;
              // yyerror(String.Format ("syntax error, got token `{0}'", yyname (yyToken)), yyExpecting(yyState));
              if (debug != null) debug.error("syntax error");
              if (yyToken == 0 /*eof*/ || yyToken == eof_token) throw new yyParser.yyUnexpectedEof ();
              goto case 1;
            case 1: case 2:
              yyErrorFlag = 3;
              do {
                if ((yyN = yySindex[yyStates[yyTop]]) != 0
                    && (yyN += Token.yyErrorCode) >= 0 && yyN < yyTable.Length
                    && yyCheck[yyN] == Token.yyErrorCode) {
                  if (debug != null)
                    debug.shift(yyStates[yyTop], yyTable[yyN], 3);
                  yyState = yyTable[yyN];
                  yyVal = yyLex.value();
                  goto continue_yyLoop;
                }
                if (debug != null) debug.pop(yyStates[yyTop]);
              } while (-- yyTop >= 0);
              if (debug != null) debug.reject();
              throw new yyParser.yyException("irrecoverable syntax error");
  
            case 3:
              if (yyToken == 0) {
                if (debug != null) debug.reject();
                throw new yyParser.yyException("irrecoverable syntax error at end-of-file");
              }
              if (debug != null)
                debug.discard(yyState, yyToken, yyname(yyToken),
  							yyLex.value());
              yyToken = -1;
              goto continue_yyDiscarded;		// leave stack alone
            }
        }
        int yyV = yyTop + 1-yyLen[yyN];
        if (debug != null)
          debug.reduce(yyState, yyStates[yyV-1], yyN, YYRules.getRule (yyN), yyLen[yyN]);
        yyVal = yyV > yyTop ? null : yyVals[yyV]; // yyVal = yyDefault(yyV > yyTop ? null : yyVals[yyV]);
        switch (yyN) {
case 1:
#line 85 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = SetEcmaDescType (yyVals[0+yyTop], EcmaDesc.Kind.Type); }
  break;
case 2:
#line 86 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = SetEcmaDescType (yyVals[0+yyTop], EcmaDesc.Kind.Namespace); }
  break;
case 3:
#line 87 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = SetEcmaDescType (yyVals[0+yyTop], EcmaDesc.Kind.Method); }
  break;
case 4:
#line 88 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = SetEcmaDescType (yyVals[0+yyTop], EcmaDesc.Kind.Field); }
  break;
case 5:
#line 89 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = SetEcmaDescType (yyVals[0+yyTop], EcmaDesc.Kind.Constructor); }
  break;
case 6:
#line 90 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = SetEcmaDescType (yyVals[0+yyTop], EcmaDesc.Kind.Property); }
  break;
case 7:
#line 91 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = SetEcmaDescType (yyVals[0+yyTop], EcmaDesc.Kind.Event); }
  break;
case 8:
#line 92 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = SetEcmaDescType (yyVals[0+yyTop], EcmaDesc.Kind.Operator); }
  break;
case 9:
#line 96 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = new List<string> { (string)yyVals[0+yyTop] }; }
  break;
case 10:
#line 97 "Monodoc.Ecma/EcmaUrlParser.jay"
  { ((ICollection<string>)yyVals[0+yyTop]).Add ((string)yyVals[-2+yyTop]); yyVal = yyVals[0+yyTop]; }
  break;
case 11:
#line 100 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = new EcmaDesc { Namespace = string.Join (".", ((IEnumerable<string>)yyVals[0+yyTop]).Reverse ()) }; }
  break;
case 12:
  case_12();
  break;
case 13:
  case_13();
  break;
case 14:
  case_14();
  break;
case 15:
#line 142 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = null; }
  break;
case 16:
#line 143 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = yyVals[0+yyTop]; }
  break;
case 17:
#line 146 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = null; }
  break;
case 18:
#line 147 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = Enumerable.Repeat<EcmaDesc> (null, (int)yyVals[0+yyTop]).ToList (); }
  break;
case 19:
#line 148 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = yyVals[-1+yyTop]; }
  break;
case 20:
#line 151 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = new List<EcmaDesc> () { (EcmaDesc)yyVals[0+yyTop] }; }
  break;
case 21:
#line 152 "Monodoc.Ecma/EcmaUrlParser.jay"
  { ((List<EcmaDesc>)yyVals[-2+yyTop]).Add ((EcmaDesc)yyVals[0+yyTop]); yyVal = yyVals[-2+yyTop]; }
  break;
case 22:
#line 155 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = null; }
  break;
case 23:
#line 156 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = "?"; }
  break;
case 24:
#line 159 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = null; }
  break;
case 25:
  case_25();
  break;
case 26:
#line 167 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = 1; }
  break;
case 27:
#line 168 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = ((int)yyVals[0+yyTop]) + 1; }
  break;
case 28:
#line 171 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = null; }
  break;
case 29:
#line 172 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = Tuple.Create<char, string> (((string)yyVals[0+yyTop])[0], null); }
  break;
case 30:
#line 173 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = Tuple.Create<char, string> (((string)yyVals[-2+yyTop])[0], (string)yyVals[0+yyTop]); }
  break;
case 31:
#line 177 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = "*"; }
  break;
case 32:
#line 178 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = yyVals[0+yyTop]; }
  break;
case 33:
  case_33();
  break;
case 34:
  case_34();
  break;
case 35:
  case_35();
  break;
case 36:
#line 206 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = (string)yyVals[-1+yyTop] + (yyVals[0+yyTop] == null ? string.Empty : "<" + string.Join (",", ((IEnumerable<EcmaDesc>)yyVals[0+yyTop]).Select (t => t.ToCompleteTypeName ())) + ">"); }
  break;
case 37:
  case_37();
  break;
case 38:
#line 214 "Monodoc.Ecma/EcmaUrlParser.jay"
  { var desc = (EcmaDesc)yyVals[-1+yyTop]; desc.DescModifier = (EcmaDesc.Mod)yyVals[0+yyTop]; yyVal = desc; }
  break;
case 39:
#line 217 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = EcmaDesc.Mod.Normal; }
  break;
case 40:
#line 218 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = EcmaDesc.Mod.Pointer; }
  break;
case 41:
#line 219 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = EcmaDesc.Mod.Ref; }
  break;
case 42:
#line 220 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = EcmaDesc.Mod.Out; }
  break;
case 43:
#line 223 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = null; }
  break;
case 44:
#line 224 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = new List<EcmaDesc> () { (EcmaDesc)yyVals[0+yyTop] }; }
  break;
case 45:
#line 225 "Monodoc.Ecma/EcmaUrlParser.jay"
  { ((List<EcmaDesc>)yyVals[0+yyTop]).Add ((EcmaDesc)yyVals[-2+yyTop]); yyVal = yyVals[0+yyTop]; }
  break;
case 46:
  case_46();
  break;
case 47:
  case_47();
  break;
case 48:
  case_48();
  break;
case 49:
#line 250 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = yyVals[0+yyTop]; }
  break;
case 50:
#line 253 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = yyVals[0+yyTop]; }
  break;
case 51:
  case_51();
  break;
case 52:
#line 263 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = yyVals[0+yyTop]; }
  break;
case 53:
#line 271 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = null; }
  break;
case 54:
#line 272 "Monodoc.Ecma/EcmaUrlParser.jay"
  { yyVal = yyVals[-1+yyTop]; }
  break;
#line default
        }
        yyTop -= yyLen[yyN];
        yyState = yyStates[yyTop];
        int yyM = yyLhs[yyN];
        if (yyState == 0 && yyM == 0) {
          if (debug != null) debug.shift(0, yyFinal);
          yyState = yyFinal;
          if (yyToken < 0) {
            yyToken = yyLex.advance() ? yyLex.token() : 0;
            if (debug != null)
               debug.lex(yyState, yyToken,yyname(yyToken), yyLex.value());
          }
          if (yyToken == 0) {
            if (debug != null) debug.accept(yyVal);
            return yyVal;
          }
          goto continue_yyLoop;
        }
        if (((yyN = yyGindex[yyM]) != 0) && ((yyN += yyState) >= 0)
            && (yyN < yyTable.Length) && (yyCheck[yyN] == yyState))
          yyState = yyTable[yyN];
        else
          yyState = yyDgoto[yyM];
        if (debug != null) debug.shift(yyStates[yyTop], yyState);
	 goto continue_yyLoop;
      continue_yyDiscarded: ;	// implements the named-loop continue: 'continue yyDiscarded'
      }
    continue_yyLoop: ;		// implements the named-loop continue: 'continue yyLoop'
    }
  }

/*
 All more than 3 lines long rules are wrapped into a method
*/
void case_12()
#line 103 "Monodoc.Ecma/EcmaUrlParser.jay"
{
                         var dotExpr = ((List<string>)yyVals[-1+yyTop]);
                         dotExpr.Reverse ();
                         var desc = yyVals[0+yyTop] as EcmaDesc;
                         desc.DescKind = EcmaDesc.Kind.Type;
                         desc.Namespace = string.Join (".", dotExpr.Take (dotExpr.Count - 1));
                         desc.TypeName = dotExpr.Last ();
                         yyVal = desc;
                     }

void case_13()
#line 115 "Monodoc.Ecma/EcmaUrlParser.jay"
{
                         var desc = yyVals[0+yyTop] as EcmaDesc;
                         desc.DescKind = EcmaDesc.Kind.Type;
                         desc.TypeName = yyVals[-1+yyTop] as string;
                         yyVal = desc;
                     }

void case_14()
#line 123 "Monodoc.Ecma/EcmaUrlParser.jay"
{
                         bool nestedDescHasEtc = yyVals[-4+yyTop] != null && ((EcmaDesc)yyVals[-4+yyTop]).IsEtc;
                         EcmaDesc nestedType = (EcmaDesc)yyVals[-4+yyTop];
                         yyVal = new EcmaDesc {
                            GenericTypeArguments = yyVals[-5+yyTop] as List<EcmaDesc>,
                            NestedType = nestedType,
                            DescIsNullable = yyVals[-3+yyTop] != null,
                            ArrayDimensions = SafeReverse (yyVals[-2+yyTop] as List<int>),
                            ArrayIsNullable = yyVals[-1+yyTop] != null,
                            Etc = yyVals[0+yyTop] != null ? ((Tuple<char, string>)yyVals[0+yyTop]).Item1 : nestedDescHasEtc ? nestedType.Etc : (char)0,
                            EtcFilter = yyVals[0+yyTop] != null ? ((Tuple<char, string>)yyVals[0+yyTop]).Item2 : nestedDescHasEtc ? nestedType.EtcFilter : null
                         };
                         if (nestedDescHasEtc) {
                            nestedType.Etc = (char)0;
                            nestedType.EtcFilter = null;
                         }
                     }

void case_25()
#line 160 "Monodoc.Ecma/EcmaUrlParser.jay"
{
                      var dims = ((IList<int>)yyVals[0+yyTop]) ?? new List<int> (2);
                      dims.Add ((int)yyVals[-2+yyTop]);
                      yyVal = dims;
                }

void case_33()
#line 181 "Monodoc.Ecma/EcmaUrlParser.jay"
{
                      var desc = yyVals[-4+yyTop] as EcmaDesc;
                      desc.MemberName = yyVals[-2+yyTop] as string;
                      desc.GenericMemberArguments = yyVals[-1+yyTop] as List<EcmaDesc>;
                      desc.MemberArguments = SafeReverse (yyVals[0+yyTop] as List<EcmaDesc>);
                      yyVal = desc;
                }

void case_34()
#line 188 "Monodoc.Ecma/EcmaUrlParser.jay"
{
                      var dotExpr = ((List<string>)yyVals[-2+yyTop]);
                      yyVal = new EcmaDesc {
                           Namespace = string.Join (".", dotExpr.Skip (2).DefaultIfEmpty (string.Empty).Reverse ()),
                           TypeName = dotExpr.Skip (1).First (),
                           MemberName = dotExpr.First (),
                           GenericMemberArguments = yyVals[-1+yyTop] as List<EcmaDesc>,
                           MemberArguments = SafeReverse (yyVals[0+yyTop] as List<EcmaDesc>)
                      };
                }

void case_35()
#line 198 "Monodoc.Ecma/EcmaUrlParser.jay"
{
                      var desc = yyVals[-2+yyTop] as EcmaDesc;
                      desc.ExplicitImplMember = yyVals[0+yyTop] as EcmaDesc;
                      yyVal = desc;
                }

void case_37()
#line 207 "Monodoc.Ecma/EcmaUrlParser.jay"
{
                      var existing = yyVals[0+yyTop] as string;
                      var expr = (string)yyVals[-3+yyTop] + (yyVals[-2+yyTop] == null ? string.Empty : "<" + string.Join (",", ((IEnumerable<EcmaDesc>)yyVals[-2+yyTop]).Select (t => t.ToCompleteTypeName ())) + ">");
                      yyVal = expr + "." + existing;
                }

void case_46()
#line 228 "Monodoc.Ecma/EcmaUrlParser.jay"
{
                 var dotExpr = ((List<string>)yyVals[0+yyTop]);
                 dotExpr.Reverse ();

                 yyVal = new EcmaDesc {
                      Namespace = dotExpr.Count > 2 ? string.Join (".", dotExpr.Take (dotExpr.Count - 2)) : string.Empty,
                      TypeName = dotExpr.Count > 1 ?  dotExpr[dotExpr.Count - 2] : string.Empty,
                      MemberName = dotExpr[dotExpr.Count - 1]
                 };
             }

void case_47()
#line 238 "Monodoc.Ecma/EcmaUrlParser.jay"
{
                 var desc = yyVals[-2+yyTop] as EcmaDesc;
                 desc.MemberName = yyVals[0+yyTop] as string;
                 yyVal = desc;
             }

void case_48()
#line 243 "Monodoc.Ecma/EcmaUrlParser.jay"
{
                 var desc = yyVals[-2+yyTop] as EcmaDesc;
                 desc.ExplicitImplMember = yyVals[0+yyTop] as EcmaDesc;
                 yyVal = desc;
             }

void case_51()
#line 256 "Monodoc.Ecma/EcmaUrlParser.jay"
{
                 var desc = yyVals[-1+yyTop] as EcmaDesc;
                 (desc.ExplicitImplMember ?? desc).MemberArguments = SafeReverse (yyVals[0+yyTop] as List<EcmaDesc>);
                 yyVal = desc;
             }

#line default
   static readonly short [] yyLhs  = {              -1,
    0,    0,    0,    0,    0,    0,    0,    0,    8,    8,
    2,    1,   10,    9,   12,   12,   11,   11,   11,   16,
   16,   13,   13,   14,   14,   17,   17,   15,   15,   15,
   18,   18,    3,    3,    3,   19,   19,   21,   22,   22,
   22,   22,   23,   23,   23,    4,    4,    4,    5,    7,
    6,   24,   20,   20,
  };
   static readonly short [] yyLen = {           2,
    3,    3,    3,    3,    3,    3,    3,    3,    1,    3,
    1,    2,    2,    6,    0,    2,    0,    2,    3,    1,
    3,    0,    1,    0,    4,    0,    2,    0,    2,    4,
    1,    1,    5,    3,    3,    2,    4,    2,    0,    1,
    1,    1,    0,    1,    3,    1,    3,    3,    1,    1,
    2,    1,    0,    3,
  };
   static readonly short [] yyDefRed = {            0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    1,    0,
    2,   11,    0,    3,    0,    0,    4,    0,   49,    5,
    0,    6,    7,   50,    8,    0,    0,    0,   12,    0,
    0,    0,    0,    0,    0,    0,   52,   51,   10,   20,
    0,   18,    0,    0,    0,   35,   34,   47,   48,    0,
    0,    0,    0,   19,    0,   16,   23,    0,    0,   40,
   41,   42,   38,    0,   54,   21,   13,    0,    0,   33,
   45,    0,    0,    0,   27,    0,    0,   14,   25,   32,
   31,    0,    0,    0,   30,    0,    0,   37,
  };
  protected static readonly short [] yyDgoto  = {             9,
   23,   21,   24,   27,   30,   32,   35,   20,   39,   66,
   40,   54,   68,   79,   88,   51,   83,   92,   95,   47,
   61,   73,   62,   48,
  };
  protected static readonly short [] yySindex = {          -21,
 -237, -234, -226, -221, -209, -208, -198, -197,    0, -191,
 -191, -191, -191, -191, -191, -191, -191, -190,    0, -219,
    0,    0, -243,    0, -219, -236,    0, -219,    0,    0,
 -196,    0,    0,    0,    0, -191, -191, -187,    0, -189,
 -185, -191, -241, -183, -191, -191,    0,    0,    0,    0,
 -238,    0, -182, -199, -219,    0,    0,    0,    0, -212,
 -181, -186, -191,    0, -219,    0,    0, -188, -196,    0,
    0,    0,    0, -191,    0,    0,    0, -178, -199,    0,
    0, -178, -192, -184,    0, -188, -239,    0,    0,    0,
    0, -180, -179, -219,    0, -176, -179,    0,
  };
  protected static readonly short [] yyRindex = {            0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    1,    0,   68,
    0,    0,    0,    0,  152,    0,    0,  162,    0,    0,
   86,    0,    0,    0,    0,    0,    0,    0,    0,   35,
    0,    0,  170,    0,    0, -175,    0,    0,    0,    0,
    0,    0,    0,  182,    4,    0,    0,    0,    0, -217,
 -174,    0,    0,    0,   18,    0,    0,   85,   86,    0,
    0,    0,    0, -175,    0,    0,    0, -173,  182,    0,
    0, -173,    0,  102,    0,   85,    0,    0,    0,    0,
    0,  119,    0,   52,    0,  135,    0,    0,
  };
  protected static readonly short [] yyGindex = {            0,
   -8,    0,  -11,    5,    0,    0,    0,   -2,   23,    0,
  -25,    0,   10,    6,    0,    0,    8,    0,   -1,  -27,
    0,    0,   21,    0,
  };
  protected static readonly short [] yyTable = {            43,
    9,   19,   29,   17,   26,   34,   26,   26,   22,   25,
   28,   25,   28,   28,   25,   57,   41,   17,   90,   31,
   33,   53,   63,   44,   10,   46,   64,   11,   50,   69,
   56,   42,   91,   49,   15,   12,   26,   60,   45,   25,
   13,   80,   28,   39,   37,    5,   38,    7,    4,   59,
   39,   17,   14,   15,   76,    3,    2,    8,    6,   70,
   71,   72,    1,   16,   17,   60,   18,   17,   96,   36,
   46,   52,   55,   53,   58,   65,   67,   86,   94,   74,
   78,   75,   82,   97,   24,   53,   87,   77,   84,   85,
   93,   89,   43,   44,   81,   98,   26,    0,    0,    0,
    0,   28,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,   29,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,   36,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,   17,    0,    0,    0,    0,    0,    0,    0,    0,
    0,   46,    0,    0,    0,    0,    0,    0,    0,   53,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,   22,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    9,    0,    9,    9,    9,    9,    9,    9,    9,
   17,    9,    9,    9,    9,    9,    9,   17,   17,    0,
   17,    0,   17,    0,    0,   17,   17,    0,   17,   17,
   17,   17,   17,   17,   15,   15,    0,    0,    0,   15,
    0,    0,   15,   15,    0,   15,   15,   15,   15,   15,
   15,   17,   17,    0,    0,    0,   17,    0,    0,   17,
   17,    0,   17,   17,   17,   17,   17,   17,   17,    0,
   17,    0,   17,    0,    0,   17,   17,    0,   17,   17,
   17,   17,    0,   17,   24,   24,    0,    0,    0,   24,
    0,    0,   24,    0,    0,   24,   24,   24,   24,   24,
   24,   28,   28,    0,    0,    0,   28,    0,    0,   28,
   28,    0,    0,   28,   28,   28,   28,   28,   29,   29,
    0,    0,    0,   29,    0,    0,   29,   29,    0,    0,
   29,   29,   29,   29,   29,   36,    0,    0,    0,   36,
    0,    0,   36,   36,    0,   36,   36,   36,   36,   36,
   36,   17,    0,    0,   17,    0,    0,    0,   17,    0,
   17,   17,   17,    0,   17,    0,   17,   17,   46,   15,
   17,    0,   17,    0,    0,    0,   17,   17,   15,    0,
   15,   22,   22,    0,   15,   15,   22,    0,    0,   22,
   22,    0,   22,   22,   22,   22,   22,
  };
  protected static readonly short [] yyCheck = {            25,
    0,   10,   14,    0,   13,   17,   15,   16,   11,   12,
   13,   14,   15,   16,   17,   43,  260,    0,  258,   15,
   16,  263,  261,  260,  262,  267,  265,  262,   37,   55,
   42,  275,  272,   36,    0,  262,   45,   46,  275,   42,
  262,   69,   45,  261,  264,   67,  266,   69,   70,   45,
  268,    0,  262,  262,   63,   77,   78,   79,   80,  272,
  273,  274,   84,  262,  262,   74,  258,    0,   94,  260,
  267,  259,  258,  263,  258,  258,  276,  270,  258,  261,
  269,  268,  261,  260,    0,    0,  271,   65,   79,   82,
  271,   86,  268,  268,   74,   97,  270,   -1,   -1,   -1,
   -1,    0,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,    0,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,    0,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,    0,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,    0,   -1,   -1,   -1,   -1,   -1,   -1,   -1,    0,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,    0,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  261,   -1,  263,  264,  265,  266,  267,  268,  269,
  267,  271,  272,  273,  274,  275,  276,  260,  261,   -1,
  263,   -1,  265,   -1,   -1,  268,  269,   -1,  271,  272,
  273,  274,  275,  276,  260,  261,   -1,   -1,   -1,  265,
   -1,   -1,  268,  269,   -1,  271,  272,  273,  274,  275,
  276,  260,  261,   -1,   -1,   -1,  265,   -1,   -1,  268,
  269,   -1,  271,  272,  273,  274,  275,  276,  261,   -1,
  263,   -1,  265,   -1,   -1,  268,  269,   -1,  271,  272,
  273,  274,   -1,  276,  260,  261,   -1,   -1,   -1,  265,
   -1,   -1,  268,   -1,   -1,  271,  272,  273,  274,  275,
  276,  260,  261,   -1,   -1,   -1,  265,   -1,   -1,  268,
  269,   -1,   -1,  272,  273,  274,  275,  276,  260,  261,
   -1,   -1,   -1,  265,   -1,   -1,  268,  269,   -1,   -1,
  272,  273,  274,  275,  276,  261,   -1,   -1,   -1,  265,
   -1,   -1,  268,  269,   -1,  271,  272,  273,  274,  275,
  276,  260,   -1,   -1,  263,   -1,   -1,   -1,  267,   -1,
  269,  260,  271,   -1,  263,   -1,  275,  276,  267,  260,
  269,   -1,  271,   -1,   -1,   -1,  275,  276,  269,   -1,
  271,  260,  261,   -1,  275,  276,  265,   -1,   -1,  268,
  269,   -1,  271,  272,  273,  274,  275,
  };

#line 274 "Monodoc.Ecma/EcmaUrlParser.jay"


}
#line default
namespace yydebug {
        using System;
	 internal interface yyDebug {
		 void push (int state, Object value);
		 void lex (int state, int token, string name, Object value);
		 void shift (int from, int to, int errorFlag);
		 void pop (int state);
		 void discard (int state, int token, string name, Object value);
		 void reduce (int from, int to, int rule, string text, int len);
		 void shift (int from, int to);
		 void accept (Object value);
		 void error (string message);
		 void reject ();
	 }
	 
	 class yyDebugSimple : yyDebug {
		 void println (string s){
			 Console.Error.WriteLine (s);
		 }
		 
		 public void push (int state, Object value) {
			 println ("push\tstate "+state+"\tvalue "+value);
		 }
		 
		 public void lex (int state, int token, string name, Object value) {
			 println("lex\tstate "+state+"\treading "+name+"\tvalue "+value);
		 }
		 
		 public void shift (int from, int to, int errorFlag) {
			 switch (errorFlag) {
			 default:				// normally
				 println("shift\tfrom state "+from+" to "+to);
				 break;
			 case 0: case 1: case 2:		// in error recovery
				 println("shift\tfrom state "+from+" to "+to
					     +"\t"+errorFlag+" left to recover");
				 break;
			 case 3:				// normally
				 println("shift\tfrom state "+from+" to "+to+"\ton error");
				 break;
			 }
		 }
		 
		 public void pop (int state) {
			 println("pop\tstate "+state+"\ton error");
		 }
		 
		 public void discard (int state, int token, string name, Object value) {
			 println("discard\tstate "+state+"\ttoken "+name+"\tvalue "+value);
		 }
		 
		 public void reduce (int from, int to, int rule, string text, int len) {
			 println("reduce\tstate "+from+"\tuncover "+to
				     +"\trule ("+rule+") "+text);
		 }
		 
		 public void shift (int from, int to) {
			 println("goto\tfrom state "+from+" to "+to);
		 }
		 
		 public void accept (Object value) {
			 println("accept\tvalue "+value);
		 }
		 
		 public void error (string message) {
			 println("error\t"+message);
		 }
		 
		 public void reject () {
			 println("reject");
		 }
		 
	 }
}
// %token constants
 class Token {
  public const int ERROR = 257;
  public const int IDENTIFIER = 258;
  public const int DIGIT = 259;
  public const int DOT = 260;
  public const int COMMA = 261;
  public const int COLON = 262;
  public const int INNER_TYPE_SEPARATOR = 263;
  public const int OP_GENERICS_LT = 264;
  public const int OP_GENERICS_GT = 265;
  public const int OP_GENERICS_BACKTICK = 266;
  public const int OP_OPEN_PAREN = 267;
  public const int OP_CLOSE_PAREN = 268;
  public const int OP_ARRAY_OPEN = 269;
  public const int OP_ARRAY_CLOSE = 270;
  public const int SLASH_SEPARATOR = 271;
  public const int STAR = 272;
  public const int REF_ARG = 273;
  public const int OUT_ARG = 274;
  public const int EXPLICIT_IMPL_SEP = 275;
  public const int QUESTION_MARK = 276;
  public const int yyErrorCode = 256;
 }
 namespace yyParser {
  using System;
  /** thrown for irrecoverable syntax errors and stack overflow.
    */
  internal class yyException : System.Exception {
    public yyException (string message) : base (message) {
    }
  }
  internal class yyUnexpectedEof : yyException {
    public yyUnexpectedEof (string message) : base (message) {
    }
    public yyUnexpectedEof () : base ("") {
    }
  }

  /** must be implemented by a scanner object to supply input to the parser.
    */
  internal interface yyInput {
    /** move on to next token.
        @return false if positioned beyond tokens.
        @throws IOException on input error.
      */
    bool advance (); // throws java.io.IOException;
    /** classifies current token.
        Should not be called if advance() returned false.
        @return current %token or single character.
      */
    int token ();
    /** associated with current token.
        Should not be called if advance() returned false.
        @return value for token().
      */
    Object value ();
  }
 }
} // close outermost namespace, that MUST HAVE BEEN opened in the prolog
