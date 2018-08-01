using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.IO;

namespace XSD_Schema_Parser
{

    public struct CSElement
    {
        public string   name;
        public string   parentName;
        public string   type;
        public  int     depth;
    };

    public class XsdParser
    {

        private static int CompareCSElements( CSElement el1, CSElement el2 )
        {
           
            if( el1.depth == el2.depth && el1.parentName == el2.parentName )
                return 0;

            if( el1.depth > el2.depth )
                return -1;

            if( el2.depth > el1.depth )
                return 1;

            if( el1.depth == el2.depth )
            {
                return string.Compare( el1.parentName, el2.parentName );
            }// if

            return 0;
        }

        static string strErrs = "";

        public void ParseXsd( string xsdAsText, ref string result )
        {
            string strResult = "";

            List<CSElement> arrElements = new List<CSElement>();
            try
            {
                LoadCSElementsArrayFromXsd( xsdAsText, ref arrElements );
            }
            catch( Exception ex )
            {
                result = ex.Message;
                return;
            }

            arrElements.Sort( CompareCSElements  );

            PrintStructs( arrElements, ref result );
            PrintClassHeaders( arrElements, ref result );
            PrintClassImplementation( arrElements, ref result );

            result += strResult;
            result += "\r\n\r\n";
            result += strErrs;
        }

        private void PrintClassImplementation( List<CSElement> arrElements, ref string sourceFileContent )
        {
            sourceFileContent += "\r\n";
            sourceFileContent += "\r\n";
            sourceFileContent += "/////////////////////////////////////////////////////////////////////////////////////\r\n";
            sourceFileContent += "// ....XmlSerializable.cpp\r\n";
            sourceFileContent += "#include \"stdafx.h\" \r\n";
            sourceFileContent += "#include \"TypeInfo.h\" \r\n";
            sourceFileContent += "#include \"....XmlSerializable.h\" \r\n";

            string curType = "";
            string memberName = "";
            string infoData = "";

            foreach( CSElement el in arrElements )
            {
                if( string.IsNullOrEmpty( el.parentName ) )
                    continue;

                // change of parent type
                if( el.parentName != curType )
                {
                    if( curType != "" )
                    {
                        sourceFileContent += "}\r\n";
                    }// if

                    sourceFileContent += "\r\n";
                    sourceFileContent += string.Format( "///////////////////////////////////////////////////////////////////////////\r\n" );
                    sourceFileContent += string.Format( "/// C{0}XmlSerializable \r\n", GetPascalCaseName( el.parentName ) );
                    sourceFileContent += string.Format( "C{0}XmlSerializable::C{0}XmlSerializable() \r\n", GetPascalCaseName( el.parentName ) );
                    sourceFileContent += "{\r\n";
                    sourceFileContent += "}\r\n";
                    sourceFileContent += "\r\n";

                    sourceFileContent += string.Format( "void C{0}XmlSerializable::FillTypeInfo() \r\n", GetPascalCaseName( el.parentName ) );
                    sourceFileContent += "{\r\n";
                    

                }// if el.parentName != curType // change of parent type

                curType = el.parentName;
                

                memberName = GetCSPrefix( el.type ) + GetPascalCaseName( el.name );
                infoData = IsSimple( el.type ) ? GetCSXmlDataType( el.type )
                        : string.Format( "C{0}XmlSerializable().GetTypeInfo()", GetPascalCaseName( el.name ) );

                sourceFileContent += string.Format( "\t{0}( {1},\t\t\"{2}\",\toffsetof( {3}, {4} ) );\r\n"
                                                    , GetTypeInfoMethodName( el.type )
                                                    , infoData
                                                    , el.name
                                                    , el.parentName.ToUpper() 
                                                    , memberName );
            }// for

            if( arrElements.Count > 0 )
            {
                sourceFileContent += "}\r\n";
            }// if
        }

        private void PrintClassHeaders( List<CSElement> arrElements, ref string headerFileContent )
        {
            string curType = "";
            int count = arrElements.Count;
            for( int i = 0; i < count; i++ )
            {
                CSElement el = arrElements[i];

                if( string.IsNullOrEmpty( el.parentName ) )
                    continue;

                // change of parent type
                if( el.parentName != curType )
                {

                    headerFileContent += "\r\n";
                    headerFileContent += string.Format( "/// <summary> C{0}XmlSerializable  </summary>\r\n", GetPascalCaseName( el.parentName ) );
                    headerFileContent += string.Format( "class DLL_EXP C{0}XmlSerializable : public CXMLSerializable \r\n", GetPascalCaseName( el.parentName ) );
                    headerFileContent += "{\r\n";
                    headerFileContent += "\t\r\n";
                    headerFileContent += "public:\r\n";
                    headerFileContent += "\t/// <summary>Стандартен конструктор</summary>\r\n";
                    headerFileContent += string.Format( "\tC{0}XmlSerializable();", GetPascalCaseName( el.parentName ) );
                    headerFileContent += "\r\n";
                    headerFileContent += "\r\n";

                    

                    if( i == count - 2 )
                    {
                        headerFileContent += string.Format(
                               "\t// CXMLSerializable имплементация\r\n" +
                               "\tLPCTSTR GetTypeName() const {{ return \"{0}\"; }}; \r\n\r\n" +
                               "\tconst long GetSize() const {{ return sizeof( m_rec{1} ); }}\r\n\r\n" +
                               "\tvirtual void * GetPoiner()  {{ return (void*)&m_rec{1}; }}\r\n\r\n" +
                               "\tvoid FillTypeInfo();\r\n", el.parentName,
                               GetPascalCaseName( el.parentName ) );

                        headerFileContent += string.Format( "\r\n\t{0} m_rec{1};\r\n\r\n", el.parentName.ToUpper(), GetPascalCaseName( el.parentName ) );
                    }
                    else
                        headerFileContent += string.Format(
                                "\t// CXMLSerializable имплементация\r\n" +
                                "\tLPCTSTR GetTypeName() const {{ return \"{0}\"; }}; \r\n\r\n" +
                                "\tconst long GetSize() const {{ return sizeof( *this ); }}\r\n\r\n" +
                                "\tvirtual void * GetPoiner()  {{ return (void*)this; }}\r\n\r\n" +
                                "\tvoid FillTypeInfo();\r\n", el.parentName );


                    headerFileContent += "};\r\n";

                }// if el.parentName != curType // change of parent type
                curType = el.parentName;

            }// for
        }

        private void PrintStructs( List<CSElement> arrElements, ref string headerFileContent )
        {
            headerFileContent += "/////////////////////////////////////////////////////////////////////////////////////\r\n";
            headerFileContent += "// ....XmlSerializable.h\r\n";

            headerFileContent += "#pragma once\r\n";
            headerFileContent += "#include \"XMLSerializable.h\"\r\n";

            string curType = "";

            foreach( CSElement el in arrElements ) 
            {
                if( string.IsNullOrEmpty( el.parentName ) )
                    continue;

                // change of parent type
                if( el.parentName != curType )
                {
                    if( curType != "" )
                    {
                        headerFileContent += "\t\r\n";
                        headerFileContent += "\t/// <summary>Стандартен конструктор</summary>\r\n";
                        headerFileContent += string.Format( "   {0}() {{ SecureZeroMemory( this, sizeof( *this ) ); }};", curType.ToUpper() );
                        headerFileContent += "\r\n";
                        headerFileContent += "};\r\n";
                    }// if
                   
                    headerFileContent += "\r\n";
                    headerFileContent += string.Format( "/// <summary> Структура {0}  </summary>\r\n", el.parentName.ToUpper() );
                    headerFileContent += string.Format( "struct DLL_EXP {0}\r\n", el.parentName.ToUpper() );
                    headerFileContent += "{\r\n";

                }// if el.parentName != curType // change of parent type

                curType = el.parentName;

                headerFileContent += "\t/// <summary></summary>\r\n";
                headerFileContent += string.Format( "\t{0}\t{1}{2};\r\n"
                                                    , GetCSType( el.type == "" ? el.name : el.type  )
                                                    , GetCSPrefix( el.type )
                                                    , GetPascalCaseName( el.name ) );
            }// for

            if( arrElements.Count > 0 )
            {
                headerFileContent += "\r\n";
                headerFileContent += "};\r\n";
            }// if
        }

        public string GetCamelCaseName( string name )
        {
            if( name == null )
                return "";
            if( name.Equals( name.ToUpper() ) )
                return name.ToLower();
            else
                return name.Substring( 0, 1 ).ToLower() + name.Substring( 1 );
        }

        public string GetPascalCaseName( string name )
        {
            if( string.IsNullOrEmpty( name ) )
                return "";

            if( name.IndexOf( "_" ) >= 0 )
            {
                name = name.Substring( 0, 1 ).ToUpper() + name.Substring( 1 ).ToLower();

                while( name.IndexOf( "_" ) >= 0 )
                {
                    int index = name.IndexOf( "_" );

                    if( index == name.Length - 1 )
                        return name.Replace( "_", "" );

                    name = name.Substring( 0, index ) + name.Substring( index + 1 );
                    name = name.Substring( 0, index ) + name.ToUpper()[index].ToString() + name.Substring( index + 1 );
                }//while

            }// if

            if( name.Substring( 1 ) == name.Substring( 1 ).ToUpper() )
                return name.Substring( 0, 1 ).ToUpper() + name.Substring( 1 ).ToLower();

            return name.Substring( 0, 1 ).ToUpper() + name.Substring( 1 );
        }

        private string GetTypeInfoMethodName( string xmlType )
        {
            if( IsSimple( xmlType ) )
                return "AddSimpleInfo";

            return "AddComplexInfo";
        }

        private string GetCSXmlDataType( string xmlType )
        {
            string result = "";
            switch( xmlType )
            {
                case "string":
                    result = "XMLDataTypeCString";
                    break;
                case "double":
                    result = "XMLDataTypeDouble";
                    break;
                case "boolean":
                    result = "XMLDataTypeBool";
                    break;
                case "float":
                    result = "XMLDataTypeFloat";
                    break;
                case "dateTime":
                    result = "XMLDataTypeFDATE";
                    break;
                case "time":
                    result = "XMLDataTypeFDATE";
                    break;
                case "date":
                    result = "XMLDataTypeFDATE";
                    break;
                case "decimal":
                    result = "XMLDataTypeDouble";
                    break;
                case "int":
                case "integer":
                    result = "XMLDataTypeInt";
                    break;
            }

            return result;
        }



        private string GetCSPrefix( string xmlType )
        {
            string result = "";
            switch( xmlType )
            {
                case "string":
                    result = "str";
                    break;
                case "double":
                    result = "d";
                    break;
                case "boolean":
                    result = "b";
                    break;
                case "float":
                    result = "f";
                    break;
                case "dateTime":
                    result = "dt";
                    break;
                case "time":
                    result = "t";
                    break;
                case "date":
                    result = "ld";
                    break;
                case "decimal":
                    result = "f";
                    break;
                case "int":
                case "integer":
                    result = "n";
                    break;
                default:
                    result = "rec";
                    break;
            }

            return result;
        }

        private bool IsSimple( string xmlType )
        {
            bool result = false;
            switch( xmlType )
            {
                case "string":
                case "double":
                case "boolean":
                case "float":
                case "dateTime":
                case "time":
                case "date":
                case "decimal":
                case "int":
                case "integer":
                    result= true;
                    break;
                default:
                    result = false;
                    break;
            }

            return result;
        }


        private string GetCSType( string xmlType )
        {
            string result = "";
            switch( xmlType )
            {
                case "string":
                    result = "CString";
                    break;
                case "double":
                    result ="double";
                    break;
                case "boolean":
                    result = "bool";
                    break;
                case "float":
                    result = "float";
                    break;
                case "dateTime":
                    result = "FDATE";
                    break;
                case "time":
                    result = "CTime";
                    break;
                case "date":
                    result = "LDATE";
                    break;
                case "decimal":
                    result = "float";
                    break;
                case "int":
                case "integer":
                    result = "int";
                    break;
                default:
                    result = xmlType.ToUpper();
                    break;
            }

            return result;
        }

        private void LoadCSElementsArrayFromXsd( string xsdAsText, ref List<CSElement> arrElements )
        {
            // Add the schema to a new XmlSchemaSet and compile it.
            // Any schema validation warnings and errors encountered reading or 
            // compiling the schema are handled by the ValidationEventHandler delegate.
            XmlSchemaSet schemaSet = new XmlSchemaSet();
            schemaSet.ValidationEventHandler += new ValidationEventHandler( ValidationCallback );

            StringReader strReader = new StringReader( xsdAsText );
            XmlReader reader = XmlReader.Create( strReader );

            XmlSchema schemaStr = XmlSchema.Read( reader, new ValidationEventHandler( ValidationCallback ) );
            schemaSet.Add( schemaStr );
            schemaSet.Compile();

            // Retrieve the compiled XmlSchema object from the XmlSchemaSet
            // by iterating over the Schemas property.
            XmlSchema customerSchema = null;
            foreach( XmlSchema schema in schemaSet.Schemas() )
            {
                customerSchema = schema;
                break;
            }//for

            if( customerSchema != null )
            {
                // Iterate over each XmlSchemaElement in the Values collection
                // of the Elements property.
                foreach( XmlSchemaElement element in customerSchema.Elements.Values )
                {
                    LoadCSElementsArrayFromXsdRecrusive( element, null, ref arrElements, 0 );
                }// for
            }// if
        }

        private void LoadCSElementsArrayFromXsdRecrusive( XmlSchemaElement element, XmlSchemaElement parentElement, 
                                            ref List<CSElement> arrElements, int depth )
        {
            if( element == null )
                return;

            string strType = "", strParentName = "";
            if( parentElement != null )
                strParentName = parentElement.Name;

            try
            {
                strType = element.ElementSchemaType.Datatype.ToString();
                strType = strType.Substring( strType.LastIndexOf( '_' ) + 1 );
            }
            catch( Exception ex ) { }

            CSElement csEl = new CSElement();
            csEl.name       = element.Name;
            csEl.parentName = strParentName;
            csEl.type       = strType;
            csEl.depth      = depth;
            arrElements.Add( csEl );
        
            // Get the complex type of the element.
            XmlSchemaComplexType complexType = element.ElementSchemaType as XmlSchemaComplexType;

            if( complexType == null )
                return;

            //// If the complex type has any attributes, get an enumerator 
            //// and write each attribute name to the console.
            //if( complexType.AttributeUses.Count > 0 )
            //{
            //    IDictionaryEnumerator enumerator =
            //        complexType.AttributeUses.GetEnumerator();

            //    while( enumerator.MoveNext() )
            //    {
            //        XmlSchemaAttribute attribute =
            //            (XmlSchemaAttribute)enumerator.Value;

            //        strBuf = string.Format( "{1}Attribute: {0}\r\n", attribute.Name, GetMultipliedString( " ", ( depth + 1 ) * spacePadCount ) );
            //        strOutput += strBuf;
            //    }// while
            //}// if

            // Get the sequence particle of the complex type.
            XmlSchemaSequence sequence = complexType.ContentTypeParticle as XmlSchemaSequence;

            if( sequence == null )
                return;

            foreach( XmlSchemaObject oElement in sequence.Items)
            {
                if( oElement.GetType() == typeof( XmlSchemaElement ) )
                {
                    XmlSchemaElement childElement = (XmlSchemaElement)oElement;
                    LoadCSElementsArrayFromXsdRecrusive( childElement, element, ref arrElements, depth + 1 );
                }// if

                if( oElement.GetType() == typeof( XmlSchemaChoice ) )
                {
                    foreach( XmlSchemaElement childElement in ((XmlSchemaChoice)oElement).Items )
                         LoadCSElementsArrayFromXsdRecrusive( childElement, element, ref arrElements, depth + 1 );
                }// if             
            }

        }

        static void ValidationCallback( object sender, ValidationEventArgs args )
        {
            strErrs += string.Format( "Line: {2}, {0}: {1} ", args.Severity, args.Message, args.Exception.SourceSchemaObject.LineNumber );
        }
    }
}
