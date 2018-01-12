using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using HassanS;
using System.IO;
using System.Text;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
//using OSGeo.OGR;
//using OSGeo.OSR;
//using OSGeo.GDAL;
using ProtoBuf;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace GDBProxy
{

    [Serializable]
    public class GDBProxy : MarshalByRefObject, IDBAccess
    {

        public ITable Project_Table;
        public ITable GeoObject_Table;
        public IFeatureClass Project_Polygon_FeatureClass;
        public IFeatureClass Seismic_2D_Line_FeatureClass;
        public IFeatureClass Seismic_3D_Polygon_FeatureClass;

        public IFeatureClass Well_Point_FeatureClass;
        public IFeatureClass WellTrajectory_3D_Line_FeatureClass;

        private bool isInitialized;
        private Dictionary<Int32,IGeometryCollection> ProjectEnvelopes; // OID: geometrybag

        public IWorkspace ws;
        public IFeatureWorkspace fws;
        public IWorkspaceName wsn;
        private string GDBpath;

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void FormLogPrint(string msg)
        {
            ((myForm)Program.form).LogUpdate(msg);
            //Program.form.de

        }

        public void FormLogSetLastLine(string msg)
        {
            ((myForm)Program.form).LogSetLastLine(msg);
            //Program.form.de

        }

        public void FormLogSet(string msg)
        {
            ((myForm)Program.form).LogSet(msg);
            //Program.form.de

        }

        public GDBProxy()
        {

            isInitialized = false;
        }


        public bool IsInitialized()
        {
            return isInitialized;
        }

        public void InitializeAo()
        {
            Log.Print("Acquiring ArcGIS license");
            if (!ESRI.ArcGIS.RuntimeManager.Bind(ESRI.ArcGIS.ProductCode.EngineOrDesktop))
            {
                Log.Print("Error: Could not find ArcGIS Runtime !");
                throw new Exception("Could not find ArcGIS Runtime");
            }

            AoInitialize aoLicenseInitializer = new AoInitialize();
            esriLicenseStatus lic_status = aoLicenseInitializer.Initialize(esriLicenseProductCode.esriLicenseProductCodeBasic);

            if (lic_status == esriLicenseStatus.esriLicenseCheckedOut)
            {
                Console.WriteLine("got license ");

                Log.Print("License successfully checked out");
                isInitialized = true;
                return;
            }
            else if (lic_status == esriLicenseStatus.esriLicenseAlreadyInitialized)
            {
                Console.WriteLine("got license ");

                Log.Print("License Error: License already initialized");
                throw new Exception("Could not grab license");

                return;
            }
            else if (lic_status == esriLicenseStatus.esriLicenseAvailable)
            {
                Console.WriteLine("got license ");

                Log.Print("License is available.. weird");
                return;
            }
            else if (lic_status == esriLicenseStatus.esriLicenseFailure)
            {
                Console.WriteLine("License Failure");

                Log.Print("License Error: License Failure");
                throw new Exception("Could not grab license");
                return;
            }
            else if (lic_status == esriLicenseStatus.esriLicenseNotLicensed)
            {
                Console.WriteLine("Not Licensed !");

                Log.Print("License Error: Not Licensed ");
                throw new Exception("Could not grab license");
                return;
            }
            else if (lic_status == esriLicenseStatus.esriLicenseNotInitialized)
            {
                Console.WriteLine("License Not Initialized");

                Log.Print("License Error: License not initialized");
                throw new Exception("Could not grab license");
                return;
            }
            else if (lic_status == esriLicenseStatus.esriLicenseUnavailable)
            {
                Console.WriteLine("License Unavailable");

                Log.Print("License Error: License in unavailable");
                throw new Exception("Could not grab license");
                return;
            }
            else
            {
                Log.Print("Error: Could not grab ArcGIS license.. unknown error: " + lic_status);
                System.Windows.Forms.MessageBox.Show("This application could not grab a license.");
                throw new Exception("Could not grab a license");
            }


        }


        private void create_GeoObject_table()
        {

            FormLogPrint("creating GeoObjects table");
            IObjectClassDescription ocDescription = new ObjectClassDescriptionClass();

            IFields GeoObjects_Table_Fields = ocDescription.RequiredFields;
            IFieldsEdit GeoObjects_Table_Fields_Edit = GeoObjects_Table_Fields as IFieldsEdit;

            ESRI.ArcGIS.Geodatabase.IFieldEdit2 GeoObjects_Table_OID_Field = (IFieldEdit2)GeoObjects_Table_Fields_Edit.get_Field(0);
            GeoObjects_Table_OID_Field.Name_2 = "GeoObject_OID";

            //ESRI.ArcGIS.Geodatabase.IFieldEdit2 GeoObjects_Table_Shape_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 GeoObjects_Table_ProjectOID_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 GeoObjects_Table_Type_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 GeoObjects_Table_Name_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            //ESRI.ArcGIS.Geodatabase.IFieldEdit2 GeoObjects_Table_Country_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 GeoObjects_Table_Path_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 GeoObjects_Table_TimeStamp_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();

            GeoObjects_Table_ProjectOID_Field.Name_2 = "Project_OID";
            GeoObjects_Table_ProjectOID_Field.Type_2 = esriFieldType.esriFieldTypeInteger;
            GeoObjects_Table_ProjectOID_Field.IsNullable_2 = false;
            GeoObjects_Table_ProjectOID_Field.Required_2 = true;
            GeoObjects_Table_ProjectOID_Field.DefaultValue_2 = -1;
            GeoObjects_Table_ProjectOID_Field.Length_2 = 55;


            GeoObjects_Table_Name_Field.Name_2 = "GeoObject_Name";
            GeoObjects_Table_Name_Field.Type_2 = esriFieldType.esriFieldTypeString;
            GeoObjects_Table_Name_Field.IsNullable_2 = false;
            GeoObjects_Table_Name_Field.Required_2 = true;
            GeoObjects_Table_Name_Field.DefaultValue_2 = "UNKNOWN";
            GeoObjects_Table_Name_Field.Length_2 = 255;

            GeoObjects_Table_Type_Field.Name_2 = "GeoObject_Type";
            GeoObjects_Table_Type_Field.Type_2 = esriFieldType.esriFieldTypeSmallInteger;
            GeoObjects_Table_Type_Field.IsNullable_2 = false;
            GeoObjects_Table_Type_Field.Required_2 = true;
            GeoObjects_Table_Type_Field.DefaultValue_2 = 0;
            GeoObjects_Table_Type_Field.Length_2 = 255;

            GeoObjects_Table_Path_Field.Name_2 = "GeoObject_Path";
            GeoObjects_Table_Path_Field.Type_2 = esriFieldType.esriFieldTypeString;
            GeoObjects_Table_Path_Field.IsNullable_2 = false;
            GeoObjects_Table_Path_Field.Required_2 = true;
            GeoObjects_Table_Path_Field.DefaultValue_2 = ".";
            GeoObjects_Table_Path_Field.Length_2 = 999;

            GeoObjects_Table_TimeStamp_Field.Name_2 = "GeoObject_Timestamp";
            GeoObjects_Table_TimeStamp_Field.Type_2 = esriFieldType.esriFieldTypeDate;
            GeoObjects_Table_TimeStamp_Field.Required_2 = false;

            //GeoObjects_Table_Shape_Field.Name_2 = "SHAPE";
            ////GeoObjects_Table_Shape_Field.Required_2 = true;
            //GeoObjects_Table_Shape_Field.Type_2 = esriFieldType.esriFieldTypeGeometry;
            //GeoObjects_Table_Shape_Field.IsNullable_2 = true;
            ////GeoObjects_Table_Shape_Field.GeometryDef_2 =
            //    IGeometryDefEdit Shape_Field_GeoDef_Edit = new GeometryDefClass();
            //Shape_Field_GeoDef_Edit.GeometryType_2 = ESRI.ArcGIS.Geometry.esriGeometryType.esriGeometryPolygon;


            GeoObjects_Table_Fields_Edit.AddField(GeoObjects_Table_ProjectOID_Field);
            GeoObjects_Table_Fields_Edit.AddField(GeoObjects_Table_Name_Field);
            GeoObjects_Table_Fields_Edit.AddField(GeoObjects_Table_Type_Field);
            //GeoObjects_Table_Fields_Edit.AddField(GeoObjects_Table_CRS_Field);
            GeoObjects_Table_Fields_Edit.AddField(GeoObjects_Table_Path_Field);
            GeoObjects_Table_Fields_Edit.AddField(GeoObjects_Table_TimeStamp_Field);


            IFieldChecker fieldChecker = new FieldCheckerClass();
            IEnumFieldError enumFieldError = null;
            IFields validatedFields = null;
            fieldChecker.Validate(GeoObjects_Table_Fields_Edit, out enumFieldError, out validatedFields);



            GeoObject_Table = fws.CreateTable("GeoObjects", GeoObjects_Table_Fields, ocDescription.ClassExtensionCLSID, null, "");


        }

        private void create_Project_table()
        {

            FormLogPrint("creating Project table");
            IObjectClassDescription ocDescription = new ObjectClassDescriptionClass();

            IFields Project_Table_Fields = ocDescription.RequiredFields;
            IFieldsEdit Project_Table_Fields_Edit = Project_Table_Fields as IFieldsEdit;


            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Project_Table_OID_Field = (IFieldEdit2)Project_Table_Fields_Edit.get_Field(0);
            Project_Table_OID_Field.Name_2 = "Project_OID";

            //ESRI.ArcGIS.Geodatabase.IFieldEdit2 Project_Table_Shape_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            //ESRI.ArcGIS.Geodatabase.IFieldEdit2 Project_Table_OID_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Project_Table_Name_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Project_Table_Country_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Project_Table_Path_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Project_Table_CRS_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Project_Table_TimeStamp_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();

            Project_Table_OID_Field.Name_2 = "Project_OID";
            Project_Table_OID_Field.Type_2 = esriFieldType.esriFieldTypeOID;
            Project_Table_OID_Field.IsNullable_2 = false;
            Project_Table_OID_Field.Required_2 = true;

            Project_Table_Name_Field.Name_2 = "Project_Name";
            Project_Table_Name_Field.Type_2 = esriFieldType.esriFieldTypeString;
            Project_Table_Name_Field.IsNullable_2 = false;
            Project_Table_Name_Field.Required_2 = true;
            Project_Table_Name_Field.DefaultValue_2 = "UNDEFINED";
            Project_Table_Name_Field.Length_2 = 55;

            Project_Table_Path_Field.Name_2 = "Project_Path";
            Project_Table_Path_Field.Type_2 = esriFieldType.esriFieldTypeString;
            Project_Table_Path_Field.IsNullable_2 = false;
            Project_Table_Path_Field.Required_2 = true;
            Project_Table_Path_Field.DefaultValue_2 = ".";
            Project_Table_Path_Field.Length_2 = 999;

            Project_Table_Country_Field.Name_2 = "Project_Country";
            Project_Table_Country_Field.Type_2 = esriFieldType.esriFieldTypeString;
            Project_Table_Country_Field.IsNullable_2 = false;
            Project_Table_Country_Field.Required_2 = true;
            Project_Table_Country_Field.DefaultValue_2 = "UNDEFINED";
            Project_Table_Country_Field.Length_2 = 30;


            //Project_Table_CRS_Field.Name_2 = "CRS_OID";
            //Project_Table_CRS_Field.Type_2 = esriFieldType.esriFieldTypeInteger;
            //Project_Table_CRS_Field.IsNullable_2 = false;
            //Project_Table_CRS_Field.Required_2 = true;
            //Project_Table_CRS_Field.DefaultValue_2 = -1; //unknown CRS


            Project_Table_TimeStamp_Field.Name_2 = "Project_Timestamp";
            Project_Table_TimeStamp_Field.Type_2 = esriFieldType.esriFieldTypeDate;
            Project_Table_TimeStamp_Field.Required_2 = false;

            //Project_Table_Shape_Field.Name_2 = "SHAPE";
            ////Project_Table_Shape_Field.Required_2 = true;
            //Project_Table_Shape_Field.Type_2 = esriFieldType.esriFieldTypeGeometry;
            //Project_Table_Shape_Field.IsNullable_2 = true;
            ////Project_Table_Shape_Field.GeometryDef_2 =
            //    IGeometryDefEdit Shape_Field_GeoDef_Edit = new GeometryDefClass();
            //Shape_Field_GeoDef_Edit.GeometryType_2 = ESRI.ArcGIS.Geometry.esriGeometryType.esriGeometryPolygon;


            //Project_Table_Fields_Edit.AddField(Project_Table_OID_Field);

            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Project_Table_CRSType_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Project_Table_CRSName_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Project_Table_CRSAuthorityName_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Project_Table_CRSAuthorityCode_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Project_Table_TransformationName_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Project_Table_TransformationAuth_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Project_Table_TransformationCode_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            
            Project_Table_CRSName_Field.Name_2 = "PROJECT_CRS_Name";
            Project_Table_CRSName_Field.Type_2 = esriFieldType.esriFieldTypeString;
            Project_Table_CRSName_Field.IsNullable_2 = false;
            Project_Table_CRSName_Field.Required_2 = true;
            Project_Table_CRSName_Field.DefaultValue_2 = "UNDEFINED";
            Project_Table_CRSName_Field.Length_2 = 250;

            Project_Table_CRSType_Field.Name_2 = "Project_CRS_TYPE";
            Project_Table_CRSType_Field.Type_2 = esriFieldType.esriFieldTypeString;
            Project_Table_CRSType_Field.IsNullable_2 = false;
            Project_Table_CRSType_Field.Required_2 = true;
            Project_Table_CRSType_Field.Length_2 = 20;

            Project_Table_CRSAuthorityName_Field.Name_2 = "Project_CRS_Auth";
            Project_Table_CRSAuthorityName_Field.Type_2 = esriFieldType.esriFieldTypeString;
            Project_Table_CRSAuthorityName_Field.IsNullable_2 = true;
            Project_Table_CRSAuthorityName_Field.Required_2 = false;
            Project_Table_CRSAuthorityName_Field.Length_2 = 20;


            Project_Table_CRSAuthorityCode_Field.Name_2 = "Project_CRS_Code";
            Project_Table_CRSAuthorityCode_Field.Type_2 = esriFieldType.esriFieldTypeString;
            Project_Table_CRSAuthorityCode_Field.IsNullable_2 = true;
            Project_Table_CRSAuthorityCode_Field.Required_2 = false;
            Project_Table_CRSAuthorityCode_Field.Length_2 = 20;

            Project_Table_TransformationName_Field.Name_2 = "Project_Transform_Name";
            Project_Table_TransformationName_Field.Type_2 = esriFieldType.esriFieldTypeString;
            Project_Table_TransformationName_Field.IsNullable_2 = true;
            Project_Table_TransformationName_Field.Required_2 = false;
            Project_Table_TransformationName_Field.Length_2 = 50;

            Project_Table_TransformationAuth_Field.Name_2 = "Project_Transform_Auth";
            Project_Table_TransformationAuth_Field.Type_2 = esriFieldType.esriFieldTypeString;
            Project_Table_TransformationAuth_Field.IsNullable_2 = true;
            Project_Table_TransformationAuth_Field.Required_2 = false;
            Project_Table_TransformationAuth_Field.Length_2 = 10;

            Project_Table_TransformationCode_Field.Name_2 = "Project_Transform_Code";
            Project_Table_TransformationCode_Field.Type_2 = esriFieldType.esriFieldTypeString;
            Project_Table_TransformationCode_Field.IsNullable_2 = true;
            Project_Table_TransformationCode_Field.Required_2 = false;
            Project_Table_TransformationCode_Field.Length_2 = 10;



            Project_Table_Fields_Edit.AddField(Project_Table_Name_Field);
            Project_Table_Fields_Edit.AddField(Project_Table_Country_Field);
            //Project_Table_Fields_Edit.AddField(Project_Table_CRS_Field);
            //Project_Table_Fields_Edit.AddField(Project_Table_Path_Field);

            Project_Table_Fields_Edit.AddField(Project_Table_CRSName_Field);
            Project_Table_Fields_Edit.AddField(Project_Table_CRSType_Field);
            Project_Table_Fields_Edit.AddField(Project_Table_CRSAuthorityName_Field);
            Project_Table_Fields_Edit.AddField(Project_Table_CRSAuthorityCode_Field);
            Project_Table_Fields_Edit.AddField(Project_Table_TransformationName_Field);
            Project_Table_Fields_Edit.AddField(Project_Table_TransformationAuth_Field);
            Project_Table_Fields_Edit.AddField(Project_Table_TransformationCode_Field);
            
            Project_Table_Fields_Edit.AddField(Project_Table_Path_Field);
            Project_Table_Fields_Edit.AddField(Project_Table_TimeStamp_Field);


            IFieldChecker fieldChecker = new FieldCheckerClass();
            IEnumFieldError enumFieldError = null;
            IFields validatedFields = null;
            fieldChecker.Validate(Project_Table_Fields_Edit, out enumFieldError, out validatedFields);


            Project_Table = fws.CreateTable("Project", Project_Table_Fields, ocDescription.ClassExtensionCLSID, null, "");


        }

        enum CRStype
        {
            UNKNOWN,
            PROJECTED
        };

        private void create_CRS_table()
        {



            IObjectClassDescription ocDescription = new ObjectClassDescriptionClass();

            IFields CRS_Table_Fields = ocDescription.RequiredFields;
            IFieldsEdit Project_Table_Fields_Edit = CRS_Table_Fields as IFieldsEdit;

            //// 0 - UNKNOWN
            //// 1 - PROJECTED
            //// 2 - GEOGRAPHIC
            //// 3 - 

            ////ESRI.ArcGIS.Geodatabase.IFieldEdit2 Project_Table_Shape_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            //ESRI.ArcGIS.Geodatabase.IFieldEdit2 CRS_Table_OID_Field;// = new ESRI.ArcGIS.Geodatabase.FieldClass();
            //ESRI.ArcGIS.Geodatabase.IFieldEdit2 CRS_Table_Type_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 CRS_Table_TypeName_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 CRS_Table_Name_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 CRS_Table_AuthorityName_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 CRS_Table_AuthorityCode_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 CRS_Table_TransformationName_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();

            CRS_Table_Name_Field.Name_2 = "Name";
            CRS_Table_Name_Field.Type_2 = esriFieldType.esriFieldTypeString;
            CRS_Table_Name_Field.IsNullable_2 = false;
            CRS_Table_Name_Field.Required_2 = true;
            CRS_Table_TypeName_Field.DefaultValue_2 = "UNKNOWN";
            CRS_Table_Name_Field.Length_2 = 250;

            CRS_Table_TypeName_Field.Name_2 = "TypeName";
            CRS_Table_TypeName_Field.Type_2 = esriFieldType.esriFieldTypeString;
            CRS_Table_TypeName_Field.IsNullable_2 = false;
            CRS_Table_TypeName_Field.Required_2 = true;
            CRS_Table_TypeName_Field.Length_2 = 20;

            CRS_Table_AuthorityName_Field.Name_2 = "AuthorityName";
            CRS_Table_AuthorityName_Field.Type_2 = esriFieldType.esriFieldTypeString;
            CRS_Table_AuthorityName_Field.IsNullable_2 = false;
            CRS_Table_AuthorityName_Field.Required_2 = true;
            CRS_Table_AuthorityName_Field.Length_2 = 20;


            CRS_Table_AuthorityCode_Field.Name_2 = "AuthorityCode";
            CRS_Table_AuthorityCode_Field.Type_2 = esriFieldType.esriFieldTypeString;
            CRS_Table_AuthorityCode_Field.IsNullable_2 = false;
            CRS_Table_AuthorityCode_Field.Required_2 = true;
            CRS_Table_AuthorityCode_Field.Length_2 = 20;

            CRS_Table_TransformationName_Field.Name_2 = "TransformCRS";
            CRS_Table_TransformationName_Field.Type_2 = esriFieldType.esriFieldTypeString;
            CRS_Table_TransformationName_Field.IsNullable_2 = false;
            CRS_Table_TransformationName_Field.Required_2 = true;
            CRS_Table_TransformationName_Field.Length_2 = 200;


            Project_Table_Fields_Edit.AddField(CRS_Table_Name_Field);
            Project_Table_Fields_Edit.AddField(CRS_Table_TypeName_Field);
            Project_Table_Fields_Edit.AddField(CRS_Table_AuthorityName_Field);
            Project_Table_Fields_Edit.AddField(CRS_Table_AuthorityCode_Field);
            Project_Table_Fields_Edit.AddField(CRS_Table_TransformationName_Field);


            IFieldChecker fieldChecker = new FieldCheckerClass();
            IEnumFieldError enumFieldError = null;
            IFields validatedFields = null;
            fieldChecker.Validate(Project_Table_Fields_Edit, out enumFieldError, out validatedFields);


            //Project_Table = fws.CreateTable("ProjectCRS", Project_Table_Fields, ocDescription.ClassExtensionCLSID, null, "");


        }

        private void create_Project_Polygon_featureclass()
        {

            FormLogPrint("creating Project_Polygon feature class");
            IObjectClassDescription ocDescription = new ObjectClassDescriptionClass();

            IFields Project_Polygon_Feature_Fields = ocDescription.RequiredFields;
            IFieldsEdit Project_Polygon_Feature_Fields_Edit = Project_Polygon_Feature_Fields as IFieldsEdit;

            ESRI.ArcGIS.Geodatabase.IFieldEdit2 ProjectPolygon_ProjectOID_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 ProjectPolygon_Shape_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();

            ProjectPolygon_ProjectOID_Field.Name_2 = "Project_OID";
            ProjectPolygon_ProjectOID_Field.Type_2 = esriFieldType.esriFieldTypeInteger;
            ProjectPolygon_ProjectOID_Field.IsNullable_2 = false;
            ProjectPolygon_ProjectOID_Field.Required_2 = true;
            ProjectPolygon_ProjectOID_Field.DefaultValue_2 = -1;
            ProjectPolygon_ProjectOID_Field.Length_2 = 55;

            ProjectPolygon_Shape_Field.Name_2 = "SHAPE";
            ProjectPolygon_Shape_Field.Required_2 = true;
            ProjectPolygon_Shape_Field.Type_2 = esriFieldType.esriFieldTypeGeometry;
            ProjectPolygon_Shape_Field.IsNullable_2 = true;
            //ProjectPolygon_Shape_Field.GeometryDef_2 =
            IGeometryDefEdit Shape_Field_GeoDef_Edit = new GeometryDefClass();
            Shape_Field_GeoDef_Edit.GeometryType_2 = ESRI.ArcGIS.Geometry.esriGeometryType.esriGeometryPolygon;
            ProjectPolygon_Shape_Field.GeometryDef_2 = (IGeometryDef)Shape_Field_GeoDef_Edit;

            ISpatialReferenceFactory2 spatialreffactory = new SpatialReferenceEnvironmentClass();
            ISpatialReference spatialrefW84 = spatialreffactory.CreateGeographicCoordinateSystem((int)esriSRGeoCSType.esriSRGeoCS_WGS1984);
            Shape_Field_GeoDef_Edit.SpatialReference_2 = spatialrefW84;

            Project_Polygon_Feature_Fields_Edit.AddField(ProjectPolygon_ProjectOID_Field);
            Project_Polygon_Feature_Fields_Edit.AddField(ProjectPolygon_Shape_Field);


            IFieldChecker fieldChecker = new FieldCheckerClass();
            IEnumFieldError enumFieldError = null;
            IFields validatedFields = null;
            fieldChecker.Validate(Project_Polygon_Feature_Fields_Edit, out enumFieldError, out validatedFields);

            Project_Polygon_FeatureClass = fws.CreateFeatureClass("Project_Polygon", Project_Polygon_Feature_Fields, ocDescription.ClassExtensionCLSID, null, esriFeatureType.esriFTSimple, ProjectPolygon_Shape_Field.Name, "");
            //Project_Table = fws.CreateTable("Project", Project_Table_Fields, ocDescription.ClassExtensionCLSID, null, "");



        }

        private void create_Seismic_3D_Polygon_featureclass()
        {

            FormLogPrint("creating Seismic_3D_Polygon feature class");
            IObjectClassDescription ocDescription = new ObjectClassDescriptionClass();

            IFields Seismic_3D_Polygon_Feature_Fields = ocDescription.RequiredFields;
            IFieldsEdit Seismic3D_Polygon_Feature_Fields_Edit = Seismic_3D_Polygon_Feature_Fields as IFieldsEdit;

            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Seismic3D_ProjectOID_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Seismic3D_GeoObjectOID_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Seismic3D_Shape_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Seismic3D_SurveyName_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            
            Seismic3D_ProjectOID_Field.Name_2 = "Project_OID";
            Seismic3D_ProjectOID_Field.Type_2 = esriFieldType.esriFieldTypeInteger;
            Seismic3D_ProjectOID_Field.IsNullable_2 = false;
            Seismic3D_ProjectOID_Field.Required_2 = true;

            Seismic3D_GeoObjectOID_Field.Name_2 = "GeoObject_OID";
            Seismic3D_GeoObjectOID_Field.Type_2 = esriFieldType.esriFieldTypeInteger;
            Seismic3D_GeoObjectOID_Field.IsNullable_2 = false;
            Seismic3D_GeoObjectOID_Field.Required_2 = true;

            Seismic3D_SurveyName_Field.Name_2 = "Seismic3DCube_name";
            Seismic3D_SurveyName_Field.Type_2 = esriFieldType.esriFieldTypeString;
            Seismic3D_SurveyName_Field.IsNullable_2 = false;
            Seismic3D_SurveyName_Field.Required_2 = true;

            Seismic3D_Shape_Field.Name_2 = "SHAPE";
            Seismic3D_Shape_Field.Required_2 = true;
            Seismic3D_Shape_Field.Type_2 = esriFieldType.esriFieldTypeGeometry;
            Seismic3D_Shape_Field.IsNullable_2 = true;
            //Seismic3D_Shape_Field.GeometryDef_2 =
            IGeometryDefEdit Shape_Field_GeoDef_Edit = new GeometryDefClass();
            Shape_Field_GeoDef_Edit.GeometryType_2 = ESRI.ArcGIS.Geometry.esriGeometryType.esriGeometryPolygon;
            Seismic3D_Shape_Field.GeometryDef_2 = (IGeometryDef)Shape_Field_GeoDef_Edit;

            ISpatialReferenceFactory2 spatialreffactory = new SpatialReferenceEnvironmentClass();
            ISpatialReference spatialrefW84 = spatialreffactory.CreateGeographicCoordinateSystem((int)esriSRGeoCSType.esriSRGeoCS_WGS1984);
            Shape_Field_GeoDef_Edit.SpatialReference_2 = spatialrefW84;

            Seismic3D_Polygon_Feature_Fields_Edit.AddField(Seismic3D_ProjectOID_Field);
            Seismic3D_Polygon_Feature_Fields_Edit.AddField(Seismic3D_GeoObjectOID_Field);
            Seismic3D_Polygon_Feature_Fields_Edit.AddField(Seismic3D_SurveyName_Field);
            Seismic3D_Polygon_Feature_Fields_Edit.AddField(Seismic3D_Shape_Field);


            IFieldChecker fieldChecker = new FieldCheckerClass();
            IEnumFieldError enumFieldError = null;
            IFields validatedFields = null;
            fieldChecker.Validate(Seismic3D_Polygon_Feature_Fields_Edit, out enumFieldError, out validatedFields);

            Seismic_3D_Polygon_FeatureClass = fws.CreateFeatureClass("Seismic3D_Polygon", Seismic_3D_Polygon_Feature_Fields, ocDescription.ClassExtensionCLSID, null, esriFeatureType.esriFTSimple, Seismic3D_Shape_Field.Name, "");
            //Project_Table = fws.CreateTable("Project", Project_Table_Fields, ocDescription.ClassExtensionCLSID, null, "");

        }

        private void create_Seismic_2D_Polyline_featureclass()
        {

            FormLogPrint("creating Seismic_2D_Polyline feature class");
            IObjectClassDescription ocDescription = new ObjectClassDescriptionClass();

            IFields Seismic_2D_Line_Feature_Fields = ocDescription.RequiredFields;
            IFieldsEdit Seismic_2D_Line_Feature_Fields_Edit = Seismic_2D_Line_Feature_Fields as IFieldsEdit;

            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Seismic_2D_Line_ProjectOID_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Seismic_2D_Line_GeoObjectOID_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Seismic_2D_Line_Shape_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Seismic_2D_Line_Name_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();

            Seismic_2D_Line_ProjectOID_Field.Name_2 = "Project_OID";
            Seismic_2D_Line_ProjectOID_Field.Type_2 = esriFieldType.esriFieldTypeInteger;
            Seismic_2D_Line_ProjectOID_Field.IsNullable_2 = false;
            Seismic_2D_Line_ProjectOID_Field.Required_2 = true;

            Seismic_2D_Line_GeoObjectOID_Field.Name_2 = "GeoObject_OID";
            Seismic_2D_Line_GeoObjectOID_Field.Type_2 = esriFieldType.esriFieldTypeInteger;
            Seismic_2D_Line_GeoObjectOID_Field.IsNullable_2 = false;
            Seismic_2D_Line_GeoObjectOID_Field.Required_2 = true;


            Seismic_2D_Line_Name_Field.Name_2 = "Seismic2D_LineName";
            Seismic_2D_Line_Name_Field.Type_2 = esriFieldType.esriFieldTypeString;
            Seismic_2D_Line_Name_Field.IsNullable_2 = false;
            Seismic_2D_Line_Name_Field.Required_2 = true;


            Seismic_2D_Line_Shape_Field.Name_2 = "SHAPE";
            Seismic_2D_Line_Shape_Field.Required_2 = true;
            Seismic_2D_Line_Shape_Field.Type_2 = esriFieldType.esriFieldTypeGeometry;
            Seismic_2D_Line_Shape_Field.IsNullable_2 = true;
            //Seismic_2D_Shape_Field.GeometryDef_2 =
            IGeometryDefEdit Shape_Field_GeoDef_Edit = new GeometryDefClass();
            Shape_Field_GeoDef_Edit.GeometryType_2 = ESRI.ArcGIS.Geometry.esriGeometryType.esriGeometryPolyline;

            ISpatialReferenceFactory2 spatialreffactory = new SpatialReferenceEnvironmentClass();
            ISpatialReference spatialrefW84 = spatialreffactory.CreateGeographicCoordinateSystem((int)esriSRGeoCSType.esriSRGeoCS_WGS1984);
            Shape_Field_GeoDef_Edit.SpatialReference_2 = spatialrefW84;

            Seismic_2D_Line_Shape_Field.GeometryDef_2 = (IGeometryDef)Shape_Field_GeoDef_Edit;


            Seismic_2D_Line_Feature_Fields_Edit.AddField(Seismic_2D_Line_ProjectOID_Field);
            Seismic_2D_Line_Feature_Fields_Edit.AddField(Seismic_2D_Line_GeoObjectOID_Field);
            Seismic_2D_Line_Feature_Fields_Edit.AddField(Seismic_2D_Line_Name_Field);
            Seismic_2D_Line_Feature_Fields_Edit.AddField(Seismic_2D_Line_Shape_Field);

            IFieldChecker fieldChecker = new FieldCheckerClass();
            IEnumFieldError enumFieldError = null;
            IFields validatedFields = null;
            fieldChecker.Validate(Seismic_2D_Line_Feature_Fields_Edit, out enumFieldError, out validatedFields);

            Seismic_2D_Line_FeatureClass = fws.CreateFeatureClass("Seismic2D_Line", Seismic_2D_Line_Feature_Fields, ocDescription.ClassExtensionCLSID, null, esriFeatureType.esriFTSimple, Seismic_2D_Line_Shape_Field.Name, "");
            //Project_Table = fws.CreateTable("Project", Project_Table_Fields, ocDescription.ClassExtensionCLSID, null, "");

        }

        private void create_Well_Point_featureclass()
        {


            FormLogPrint("creating Well_Point feature class");
            IObjectClassDescription ocDescription = new ObjectClassDescriptionClass();

            IFields Well_Point_Feature_Fields = ocDescription.RequiredFields;
            IFieldsEdit Well_Point_Feature_Fields_Edit = Well_Point_Feature_Fields as IFieldsEdit;

            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Well_Point_ProjectOID_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Well_Point_GeoObjectOID_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Well_Point_Shape_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Well_Point_Name_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Well_Point_UWI_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Well_Point_MDDF_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Well_Point_TVDDF_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Well_Point_TVDSS_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Well_Point_KBELEV_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Well_Point_DepthUnit_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Well_Point_LongW84_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Well_Point_LatW84_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Well_Point_X_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Well_Point_Y_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();
            ESRI.ArcGIS.Geodatabase.IFieldEdit2 Well_Point_CRS_Field = new ESRI.ArcGIS.Geodatabase.FieldClass();

            Well_Point_ProjectOID_Field.Name_2 = "Project_OID";
            Well_Point_ProjectOID_Field.Type_2 = esriFieldType.esriFieldTypeInteger;
            Well_Point_ProjectOID_Field.IsNullable_2 = false;
            Well_Point_ProjectOID_Field.Required_2 = true;

            Well_Point_GeoObjectOID_Field.Name_2 = "GeoObject_OID";
            Well_Point_GeoObjectOID_Field.Type_2 = esriFieldType.esriFieldTypeInteger;
            Well_Point_GeoObjectOID_Field.IsNullable_2 = false;
            Well_Point_GeoObjectOID_Field.Required_2 = true;


            Well_Point_Name_Field.Name_2 = "Well_Name";
            Well_Point_Name_Field.Type_2 = esriFieldType.esriFieldTypeString;
            Well_Point_Name_Field.IsNullable_2 = true;
            Well_Point_Name_Field.Required_2 = true;


            Well_Point_UWI_Field.Name_2 = "Well_UWI";
            Well_Point_UWI_Field.Type_2 = esriFieldType.esriFieldTypeString;
            Well_Point_UWI_Field.IsNullable_2 = true;
            Well_Point_UWI_Field.Required_2 = true;


            Well_Point_MDDF_Field.Name_2 = "MDDF";
            Well_Point_MDDF_Field.Type_2 = esriFieldType.esriFieldTypeDouble;
            Well_Point_MDDF_Field.IsNullable_2 = false;
            Well_Point_MDDF_Field.Required_2 = true;


            Well_Point_TVDDF_Field.Name_2 = "TVDDF";
            Well_Point_TVDDF_Field.Type_2 = esriFieldType.esriFieldTypeDouble;
            Well_Point_TVDDF_Field.IsNullable_2 = false;
            Well_Point_TVDDF_Field.Required_2 = true;



            Well_Point_TVDSS_Field.Name_2 = "TVDSS";
            Well_Point_TVDSS_Field.Type_2 = esriFieldType.esriFieldTypeDouble;
            Well_Point_TVDSS_Field.IsNullable_2 = false;
            Well_Point_TVDSS_Field.Required_2 = true;

            Well_Point_KBELEV_Field.Name_2 = "KB_ELEV";
            Well_Point_KBELEV_Field.Type_2 = esriFieldType.esriFieldTypeDouble;
            Well_Point_KBELEV_Field.IsNullable_2 = false;
            Well_Point_KBELEV_Field.Required_2 = true;

            Well_Point_DepthUnit_Field.Name_2 = "DepthUnit";
            Well_Point_DepthUnit_Field.Type_2 = esriFieldType.esriFieldTypeString;
            Well_Point_DepthUnit_Field.IsNullable_2 = false;
            Well_Point_DepthUnit_Field.Required_2 = true;


            Well_Point_LongW84_Field.Name_2 = "Long_W84";
            Well_Point_LongW84_Field.Type_2 = esriFieldType.esriFieldTypeDouble;
            Well_Point_LongW84_Field.IsNullable_2 = false;
            Well_Point_LongW84_Field.Required_2 = true;


            Well_Point_LatW84_Field.Name_2 = "Lat_W84";
            Well_Point_LatW84_Field.Type_2 = esriFieldType.esriFieldTypeDouble;
            Well_Point_LatW84_Field.IsNullable_2 = false;
            Well_Point_LatW84_Field.Required_2 = true;

            Well_Point_X_Field.Name_2 = "X_ORI";
            Well_Point_X_Field.Type_2 = esriFieldType.esriFieldTypeDouble;
            Well_Point_X_Field.IsNullable_2 = false;
            Well_Point_X_Field.Required_2 = true;

            Well_Point_Y_Field.Name_2 = "Y_ORI";
            Well_Point_Y_Field.Type_2 = esriFieldType.esriFieldTypeDouble;
            Well_Point_Y_Field.IsNullable_2 = false;
            Well_Point_Y_Field.Required_2 = true;

            Well_Point_CRS_Field.Name_2 = "CRS_ORI";
            Well_Point_CRS_Field.Type_2 = esriFieldType.esriFieldTypeString;
            Well_Point_CRS_Field.IsNullable_2 = true;
            Well_Point_CRS_Field.Required_2 = true;
            Well_Point_CRS_Field.Length_2 = 2000;


            Well_Point_Shape_Field.Name_2 = "SHAPE";
            Well_Point_Shape_Field.Required_2 = true;
            Well_Point_Shape_Field.Type_2 = esriFieldType.esriFieldTypeGeometry;
            Well_Point_Shape_Field.IsNullable_2 = true;
            //Seismic_2D_Shape_Field.GeometryDef_2 =
            IGeometryDefEdit Shape_Field_GeoDef_Edit = new GeometryDefClass();
            Shape_Field_GeoDef_Edit.GeometryType_2 = ESRI.ArcGIS.Geometry.esriGeometryType.esriGeometryPoint;

            ISpatialReferenceFactory2 spatialreffactory = new SpatialReferenceEnvironmentClass();
            ISpatialReference spatialrefW84 = spatialreffactory.CreateGeographicCoordinateSystem((int)esriSRGeoCSType.esriSRGeoCS_WGS1984);
            Shape_Field_GeoDef_Edit.SpatialReference_2 = spatialrefW84;

            Well_Point_Shape_Field.GeometryDef_2 = (IGeometryDef)Shape_Field_GeoDef_Edit;


            Well_Point_Feature_Fields_Edit.AddField(Well_Point_ProjectOID_Field);
            Well_Point_Feature_Fields_Edit.AddField(Well_Point_GeoObjectOID_Field);
            Well_Point_Feature_Fields_Edit.AddField(Well_Point_Name_Field);
            Well_Point_Feature_Fields_Edit.AddField(Well_Point_UWI_Field);
            Well_Point_Feature_Fields_Edit.AddField(Well_Point_MDDF_Field);
            Well_Point_Feature_Fields_Edit.AddField(Well_Point_TVDDF_Field);
            Well_Point_Feature_Fields_Edit.AddField(Well_Point_TVDSS_Field);
            Well_Point_Feature_Fields_Edit.AddField(Well_Point_KBELEV_Field);
            Well_Point_Feature_Fields_Edit.AddField(Well_Point_DepthUnit_Field);
            Well_Point_Feature_Fields_Edit.AddField(Well_Point_LongW84_Field);
            Well_Point_Feature_Fields_Edit.AddField(Well_Point_LatW84_Field);
            Well_Point_Feature_Fields_Edit.AddField(Well_Point_X_Field);
            Well_Point_Feature_Fields_Edit.AddField(Well_Point_Y_Field);
            Well_Point_Feature_Fields_Edit.AddField(Well_Point_CRS_Field);
            Well_Point_Feature_Fields_Edit.AddField(Well_Point_Shape_Field);

            IFieldChecker fieldChecker = new FieldCheckerClass();
            IEnumFieldError enumFieldError = null;
            IFields validatedFields = null;
            fieldChecker.Validate(Well_Point_Feature_Fields_Edit, out enumFieldError, out validatedFields);

            Well_Point_FeatureClass = fws.CreateFeatureClass("Well_Point", Well_Point_Feature_Fields, ocDescription.ClassExtensionCLSID, null, esriFeatureType.esriFTSimple, Well_Point_Shape_Field.Name, "");
            //Project_Table = fws.CreateTable("Project", Project_Table_Fields, ocDescription.ClassExtensionCLSID, null, "");

        }

        public void Initialize(string GDBpath, bool append)
        {
            if (isInitialized)
                return;
            Log.Print("GDBProxy Initializing");

            this.GDBpath = GDBpath;
            this.ProjectEnvelopes = new Dictionary<int, IGeometryCollection>();
            isInitialized = true;
            InitializeAo();

            //get a list of networked drives and the IP

            //DriveInfo[] di = System.IO.DriveInfo.GetDrives();

            //recreate the gdb every time indexer is run

            // create PROJECT_Table
            // create GenericObjectTable
            // create SEISMIC2D_Table
            // create WELLS_Table

            // create PROJECT_GEOMETRY_Table
            // create SEISMIC2D_GEOMETRY_Table

            string dirname = System.IO.Path.GetDirectoryName(GDBpath);
            //string gdbname = "Petrel_GIS_Index.gdb";
            string gdbfullnamewithoutextension = System.IO.Path.GetFileNameWithoutExtension(GDBpath);
            //string gdbfullnamewithextension = System.IO.Path.GetFullPath(GDBpath);
            ShapefileWorkspaceFactory wsf = new ShapefileWorkspaceFactory();
            FileGDBWorkspaceFactory wsf2 = new FileGDBWorkspaceFactory();
            IPropertySet ps = new PropertySetClass();
            IFeatureWorkspace t;


            bool isWorkspaceCreated = false;
            try
            {

                isWorkspaceCreated = System.IO.Directory.Exists(GDBpath);

                if (!append) // replace
                {
                    
                    if (isWorkspaceCreated) //GDB exists
                        System.IO.Directory.Delete(GDBpath, true);
                    wsn = wsf2.Create(dirname, gdbfullnamewithoutextension, ps, 0);
                }
                else //append
                {
                    if (!isWorkspaceCreated) //GDB not exists
                        wsn = wsf2.Create(dirname, gdbfullnamewithoutextension, ps, 0);
                }
                ws = wsf2.OpenFromFile(GDBpath, 0);

            }
            catch (Exception exc)
            {
                MessageBox.Show("Access to " + GDBpath + " denied\n" + exc.ToString());
                throw new Exception("Access Denied");
            }
            //if (!isWorkspaceCreated)
            fws = ws as IFeatureWorkspace;


            try
            {
                Project_Table = fws.OpenTable("Project");
            }
            catch (Exception proj_exc)
            {
                //Log.Print("Exception opening Project table:" + proj_exc);
                create_Project_table();
            }

            try
            {
                Project_Polygon_FeatureClass = fws.OpenFeatureClass("Project_Polygon");
            }
            catch (Exception proj_exc)
            {

                create_Project_Polygon_featureclass();

                //Log.Print("Exception opening Project feature class:" + proj_exc);
            }

            try
            {
                GeoObject_Table = fws.OpenTable("GeoObjects");
            }
            catch
            {
                try
                {
                    create_GeoObject_table();
                }
                catch (Exception proj_exc)
                {
                    Log.Print("Exception opening GeoObject table:" + proj_exc);
                }
            }

            try
            {
                Well_Point_FeatureClass = fws.OpenFeatureClass("Well_Point");
            }
            catch
            {
                try
                {
                    create_Well_Point_featureclass();
                }
                catch (Exception proj_exc)
                {
                    Log.Print("Exception opening Well_Point feature class:" + proj_exc);
                }
            }

            try
            {
                Seismic_2D_Line_FeatureClass = fws.OpenFeatureClass("Seismic2D_Line");
            }
            catch
            {
                try
                {
                    create_Seismic_2D_Polyline_featureclass();
                }
                catch (Exception proj_exc)
                {
                    Log.Print("Exception opening Seismic2D_Line feature class:" + proj_exc);
                }
            }



            try
            {
                Seismic_3D_Polygon_FeatureClass = fws.OpenFeatureClass("Seismic3D_Polygon");
            }
            catch
            {
                try
                {
                    create_Seismic_3D_Polygon_featureclass();
                }
                catch (Exception proj_exc)
                {
                    Log.Print("Exception opening Seismic3D_Polygon feature class:" + proj_exc);
                }

            }



            int i = 5;

            //throw new Exception("Initialize not implemented yet");
        }

        [Serializable]
    public class ESRIGeometry : ISerializable
    {
        public IGeometry myGeometry;

        public ESRIGeometry()
        {
            myGeometry = null;
        }

        public ESRIGeometry(IGeometry geom)
        {
            myGeometry = geom;
        }

        #region ISerializable Members

        protected ESRIGeometry(SerializationInfo info, StreamingContext context)
        {
            IXMLSerializer xmlSerializer = new XMLSerializerClass();

            string objectAsXMLStr = info.GetString("Geometry");

            object objectDeserialized = xmlSerializer.LoadFromString(objectAsXMLStr, null, null);

            myGeometry = objectDeserialized as IGeometry;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            IXMLSerializer xmlSerializer = new XMLSerializerClass();
            info.AddValue("Geometry", xmlSerializer.SaveToString(myGeometry, null, null), typeof(string));
        }

        #endregion
    }

        public object add_seismic2D_line_feature(string name, string survey_name, string feature_path_inside_project, HassanS.Polygon polygon, object project_OID)
        {
            // add all seismic 2D lines
            IRowBuffer rowbuffer = GeoObject_Table.CreateRowBuffer();
            rowbuffer.set_Value(1, project_OID);
            rowbuffer.set_Value(2, name); // parent
            rowbuffer.set_Value(3, GeoObjectType.SEISMIC_2D_LINE); // TYPE
            rowbuffer.set_Value(4, feature_path_inside_project);
            rowbuffer.set_Value(5, DateTime.Now);
            ICursor insert_cursor = GeoObject_Table.Insert(false);
            object GeoObject_OID = insert_cursor.InsertRow(rowbuffer);

            // add seismic 2D set

            IFeatureBuffer featurebuffer = Seismic_2D_Line_FeatureClass.CreateFeatureBuffer();


            IGeometryBridge2 geometryBridge2 = new GeometryEnvironmentClass();

            IPolyline seismic_2d_line = new PolylineClass();
            IPointCollection4 seismic_2d_points = seismic_2d_line as IPointCollection4;

            WKSPoint[] aWKSPointBuffer = null;
            long cPoints = polygon._points.Count; //The number of points in the first part.
            aWKSPointBuffer = new WKSPoint[cPoints];


            int j = 0;
            foreach (HassanS.Point pt in polygon._points)
            {
                aWKSPointBuffer[j].X = pt.x;
                aWKSPointBuffer[j].Y = pt.y;
                j++;
            }

            geometryBridge2.SetWKSPoints(seismic_2d_points, ref aWKSPointBuffer);


            featurebuffer.set_Value(1, project_OID);
            featurebuffer.set_Value(2, GeoObject_OID);
            featurebuffer.set_Value(3, survey_name);
            try
            {
                featurebuffer.Shape = seismic_2d_line;
                object Missing = Type.Missing;
                ProjectEnvelopes[(int)project_OID].AddGeometry(featurebuffer.Shape, ref Missing, ref Missing);

            }
            catch
            {

            }
            
            //ESRIGeometry t = new ESRIGeometry(featurebuffer.Shape);

            //MemoryStream stream = new MemoryStream();
            //// Serialzing out to a file:
            //BinaryFormatter formatter = new BinaryFormatter();
            //formatter.Serialize(stream, t);
            //or: formatter.Serialize(stream, datasets ) ;
            //stream.Close();

            //geometry = stream.ToArray();

            IFeatureCursor feature_cursor = Seismic_2D_Line_FeatureClass.Insert(false);
            object OID = feature_cursor.InsertFeature(featurebuffer);
            return OID;
        }

        public object add_seismic3D_feature(string survey_name, string feature_path_inside_project, HassanS.Polygon feature, object project_OID)
        {
            // add all seismic 2D lines
            IRowBuffer rowbuffer = GeoObject_Table.CreateRowBuffer();

            rowbuffer.set_Value(1, project_OID);
            rowbuffer.set_Value(2, survey_name);
            rowbuffer.set_Value(3, GeoObjectType.SEISMIC_3D_POLYGON); // TYPE
            rowbuffer.set_Value(4, feature_path_inside_project);
            rowbuffer.set_Value(5, DateTime.Now);
            //rowbuffer.set_Value(4, survey_name);
            ICursor insert_cursor = GeoObject_Table.Insert(false);
            object GeoObject_OID = insert_cursor.InsertRow(rowbuffer);

            // add seismic 2D set

            IGeometryBridge2 geometryBridge2 = new GeometryEnvironmentClass();

            IPolygon seismic_cube_outline = new PolygonClass();

            IPointCollection4 seismic_cube_points = seismic_cube_outline as IPointCollection4;

            WKSPoint[] aWKSPointBuffer = null;
            long cPoints = feature._points.Count; //The number of points in the first part.
            aWKSPointBuffer = new WKSPoint[cPoints+1];

            
            int j = 0;
            foreach (HassanS.Point pt in feature._points)
            {
                aWKSPointBuffer[j].X = pt.x;
                aWKSPointBuffer[j].Y = pt.y;
                j++;
            }
            aWKSPointBuffer[j].X = feature._points[0].x;
            aWKSPointBuffer[j].Y = feature._points[0].y;

            geometryBridge2.SetWKSPoints(seismic_cube_points, ref aWKSPointBuffer);


            IFeatureBuffer featurebuffer = Seismic_3D_Polygon_FeatureClass.CreateFeatureBuffer();
            featurebuffer.set_Value(2, GeoObject_OID);
            featurebuffer.set_Value(1, project_OID);
            featurebuffer.set_Value(3, survey_name);
            try
            {
                featurebuffer.Shape = seismic_cube_outline;

                object Missing = Type.Missing;
                ProjectEnvelopes[(int)project_OID].AddGeometry(featurebuffer.Shape, ref Missing, ref Missing);

            }
            catch { }

            IFeatureCursor feature_cursor = Seismic_3D_Polygon_FeatureClass.Insert(false);
            object OID = feature_cursor.InsertFeature(featurebuffer);
            return OID;
        }

        public object add_well_feature(
            string well_name,
            string well_UWI,
            double MDDF,
            double TVDDF,
            double TVDSS,
            double KB,
            string unitName,
            double LongW84,
            double LatW84,
            double X,
            double Y,
            string WKT,
            string feature_path_inside_project, HassanS.Point shape, object project_OID)
        {

            IRowBuffer rowbuffer = GeoObject_Table.CreateRowBuffer();

            rowbuffer.set_Value(1, project_OID);
            rowbuffer.set_Value(2, well_name); // parent
            rowbuffer.set_Value(3, GeoObjectType.WELL_POINT); // TYPE
            rowbuffer.set_Value(4, feature_path_inside_project);
            rowbuffer.set_Value(5, DateTime.Now);

            ICursor insert_cursor = GeoObject_Table.Insert(false);
            object GeoObject_OID = insert_cursor.InsertRow(rowbuffer);

            // add seismic 2D set

            IPoint well_point = new PointClass();
            well_point.X = shape.x;
            well_point.Y = shape.y;
            well_point.Z = 0;

            if (Double.IsInfinity(TVDDF))
                TVDDF = 0;
            if (Double.IsInfinity(TVDSS))
                TVDSS = 0;
            if (Double.IsInfinity(MDDF))
                MDDF = 0;
            if (Double.IsInfinity(KB))
                KB = 0;

            IField shape_field = Well_Point_FeatureClass.Fields.get_Field(15);

            IFeatureBuffer featurebuffer = Well_Point_FeatureClass.CreateFeatureBuffer();
            featurebuffer.set_Value(2, GeoObject_OID);
            featurebuffer.set_Value(1, project_OID);
            featurebuffer.set_Value(3, well_name);
            featurebuffer.set_Value(4, well_UWI);
            featurebuffer.set_Value(5, MDDF);
            featurebuffer.set_Value(6, TVDDF);
            featurebuffer.set_Value(7, TVDSS);
            featurebuffer.set_Value(8, KB);
            featurebuffer.set_Value(9, unitName);
            featurebuffer.set_Value(10, LongW84);
            featurebuffer.set_Value(11, LatW84);
            featurebuffer.set_Value(12, X);
            featurebuffer.set_Value(13, Y);
            featurebuffer.set_Value(14, WKT);
            try
            {
                featurebuffer.Shape = well_point;

                object Missing = Type.Missing;
                ProjectEnvelopes[(int)project_OID].AddGeometry(featurebuffer.Shape, ref Missing, ref Missing);

            }
            catch { }

            IFeatureCursor feature_cursor = Well_Point_FeatureClass.Insert(false);
            object OID = feature_cursor.InsertFeature(featurebuffer);
            return OID;

        }

        public object add_Project(string project_name, string project_path, string country_name, string CRS_WKT, string EarlyBoundTransform_WKT)
        {
            if (project_path == null || project_path == "" || project_name == null || project_name == "")
            {
                throw new Exception("Directory Path and Name cannot be empty");
            }
            if (country_name == null || country_name == "")
            {
                country_name = "UNKNOWN";
            }


            string crs_name = "NULL";
            string crs_type = "UNKNOWN";
            string crs_auth = null;
            string crs_code = null;
            string crs_transform_name=null;
            string crs_transform_auth = null;
            string crs_transform_code = null;

            try
            {
                if (CRS_WKT != null)
                {
                    ISpatialReference spatial_ref = null;
                    int bytes_read;

                    //OSGeo.OSR.CoordinateTransformation transform = new CoordinateTransformation(null, null);


                    SpatialReference spatial_ref2 = new SpatialReference(EarlyBoundTransform_WKT);

                    //spatial_ref2.gett

                    //int j = spatial_ref2.IsGeographic();
                    //j = spatial_ref2.IsProjected();
                    SpatialReferenceEnvironmentClass spatial_ref_env = new SpatialReferenceEnvironmentClass();

                    try
                    {
                        spatial_ref_env.CreateESRISpatialReference(CRS_WKT, out spatial_ref, out bytes_read);

                        ProjectedCoordinateSystem projectedCRS = spatial_ref as ProjectedCoordinateSystem;

                        IGeographicCoordinateSystem2 geographicCRS = spatial_ref as IGeographicCoordinateSystem2;


                        crs_name = spatial_ref.Name;

                        ISpatialReferenceAuthority authority = spatial_ref as ISpatialReferenceAuthority;

                        crs_auth = authority.AuthorityName;
                        crs_code = authority.Code.ToString();


                        string t = spatial_ref2.GetAttrValue("GEOGTRAN", 0);
                        if (t != null)
                        {
                            crs_transform_name = t;
                        }
                        if (projectedCRS != null) //projected
                        {
                            crs_type = "PROJECTED";
                            crs_transform_auth = spatial_ref2.GetAttrValue("AUTHORITY", 0);
                            crs_transform_code = spatial_ref2.GetAttrValue("AUTHORITY", 1);
                        }
                        else if (geographicCRS != null) //geo
                        {
                            crs_type = "GEOGRAPHIC";
                        }
                        else
                        {
                            throw new Exception("Error Loading WKT");
                        }
                    }
                    catch (Exception exc)
                    {
                        Program.form.LogUpdate("WKT not well formed: " + CRS_WKT);
                    }
                    
                }
                else
                {

                }
            }
            catch (Exception exc)
            {
                Program.form.LogUpdate("Exc: " + exc);
            }


            //Project_Table_Fields_Edit.AddField(Project_Table_Name_Field);
            //Project_Table_Fields_Edit.AddField(Project_Table_Country_Field);
            //Project_Table_Fields_Edit.AddField(Project_Table_Path_Field);

            //Project_Table_Fields_Edit.AddField(CRS_Table_Name_Field);
            //Project_Table_Fields_Edit.AddField(CRS_Table_TypeName_Field);
            //Project_Table_Fields_Edit.AddField(CRS_Table_AuthorityName_Field);
            //Project_Table_Fields_Edit.AddField(CRS_Table_AuthorityCode_Field);
            //Project_Table_Fields_Edit.AddField(CRS_Table_TransformationName_Field);

            //Project_Table_Fields_Edit.AddField(Project_Table_TimeStamp_Field);


            IRowBuffer rowbuffer = Project_Table.CreateRowBuffer();
            rowbuffer.set_Value(1, project_name);
            rowbuffer.set_Value(2, country_name);
            rowbuffer.set_Value(3, crs_name);
            rowbuffer.set_Value(4, crs_type);
            rowbuffer.set_Value(5, crs_auth);
            rowbuffer.set_Value(6, crs_code);
            rowbuffer.set_Value(7, crs_transform_name);
            rowbuffer.set_Value(8, crs_transform_auth);
            rowbuffer.set_Value(9, crs_transform_code);
            rowbuffer.set_Value(10, project_path);
            rowbuffer.set_Value(11, DateTime.Now);
            ICursor insert_cursor = Project_Table.Insert(false);
            object OID = insert_cursor.InsertRow(rowbuffer);
            ProjectEnvelopes[(int)OID] = new GeometryBagClass();
            IFeatureBuffer featurebuffer = Project_Polygon_FeatureClass.CreateFeatureBuffer();
            featurebuffer.set_Value(1, OID);
            featurebuffer.Shape = null;
            IFeatureCursor feature_cursor = Project_Polygon_FeatureClass.Insert(false);
            feature_cursor.InsertFeature(featurebuffer);
            return OID;
        }

        public object get_Project_by_path(string project_dir_path)
        {
            return null;
        }


        public object finalize_Project( object project_OID)
        {
            Int32 OID = (int)project_OID ;

            IFeature feature = Project_Polygon_FeatureClass.GetFeature(OID);
            IGeometryCollection geometry_bag = ProjectEnvelopes[(int) project_OID];
            if (geometry_bag == null || geometry_bag.GeometryCount == 0 ) return null;

            IGeometryBag geo5 = (IGeometryBag)geometry_bag;

            ISpatialReferenceFactory2 spatialreffactory = new SpatialReferenceEnvironmentClass();
            ISpatialReference spatialrefW84 = spatialreffactory.CreateGeographicCoordinateSystem((int)esriSRGeoCSType.esriSRGeoCS_WGS1984);
            geo5.SpatialReference = spatialrefW84;


            IPolygon project_envelope = new PolygonClass();

                IGeometryBridge2 geometryBridge2 = new GeometryEnvironmentClass();
                IPointCollection4 envelope_points = project_envelope as IPointCollection4;

            WKSPoint[] aWKSPointBuffer = null;
            aWKSPointBuffer = new WKSPoint[5];

            
            int j = 0;

            aWKSPointBuffer[0].X = (geometry_bag as IGeometry).Envelope.UpperLeft.X;
            aWKSPointBuffer[0].Y = (geometry_bag as IGeometry).Envelope.UpperLeft.Y;

            aWKSPointBuffer[1].X = (geometry_bag as IGeometry).Envelope.UpperRight.X;
            aWKSPointBuffer[1].Y = (geometry_bag as IGeometry).Envelope.UpperRight.Y;

            aWKSPointBuffer[2].X = (geometry_bag as IGeometry).Envelope.LowerRight.X;
            aWKSPointBuffer[2].Y = (geometry_bag as IGeometry).Envelope.LowerRight.Y;

            aWKSPointBuffer[3].X = (geometry_bag as IGeometry).Envelope.LowerLeft.X;
            aWKSPointBuffer[3].Y = (geometry_bag as IGeometry).Envelope.LowerLeft.Y;

            aWKSPointBuffer[4].X = (geometry_bag as IGeometry).Envelope.UpperLeft.X;
            aWKSPointBuffer[4].Y = (geometry_bag as IGeometry).Envelope.UpperLeft.Y;


            geometryBridge2.SetWKSPoints(envelope_points, ref aWKSPointBuffer);

            ProjectEnvelopes.Remove((int)project_OID);


            feature.Shape = project_envelope;
            feature.Store();
            GC.Collect();
            return null;
        }


        //public bool Initialize_AO(){

        //}

        ///// <summary>
        ///// Add a line feature to a feature class named Seismic2D
        ///// * unique feature_path: Petrel project.pet; SeismicProject.SeismicCollection(name A).Seismic2D(name B)
        ///// 
        ///// </summmary>
        //public FeatureReference add_seismic2D_feature(string feature_path_inside_project, Feature feature, int project_UID ){
        //    // fields seismic2D_name, shape, UID, project_UID , path_inside_project, petrel_GUID

        //}

        //public int add_Project(string server_name,string project_dir_path, string project_name) // returns project_UID
        //{
        //    // fields project_name, shape, UID, project_UID, project_dir_path, server_name
        //}

        //public FeatureReference set_Project_feature(int project_UID, Feature feature) // returns project_UID
        //{

        //}




    }

    public class myForm : Form
    {
        protected System.Windows.Forms.TextBox logtextbox;
        protected System.Windows.Forms.ToolStripLabel statustext;
        public delegate void LogUpdateEventhandler(string msg);
        protected void LogUpdate_impl(string msg)
        {
            this.logtextbox.Text += msg + System.Environment.NewLine;
            logtextbox.Select(logtextbox.Text.Length - 1, 0);

            this.logtextbox.ScrollToCaret();
        }

        public void LogSet_impl(string msg)
        {
            this.logtextbox.Text = msg + System.Environment.NewLine;

            logtextbox.Select(logtextbox.Text.Length - 1, 0);



            this.logtextbox.ScrollToCaret();
        }

        public void LogSetLastLine_impl(string msg)
        {
            //int idx = logtextbox.Text.Length - 1;
            //int curline = logtextbox.GetLineFromCharIndex(idx); // get last line number
            //int firstcharinthisline = logtextbox.GetFirstCharIndexFromLine(curline); // get first char in last line
            //logtextbox.Text = logtextbox.Text.Substring(0, firstcharinthisline);

            //logtextbox.Text += msg;
            //logtextbox.Select(logtextbox.Text.Length - 1, 0);
            this.statustext.Text = msg;
            //this.logtextbox.ScrollToCaret();
            ////logtextbox.Select(firstcharinthisline, 0);
            ////this.logtextbox.Text = msg;
        }


        public void LogUpdate(string msg)
        {
            LogUpdateEventhandler c = new LogUpdateEventhandler(LogUpdate_impl);
            this.Invoke(c, msg);
        }

        public void LogSet(string msg)
        {
            LogUpdateEventhandler c = new LogUpdateEventhandler(LogSet_impl);
            this.Invoke(c, msg);
        }

        public void LogSetLastLine(string msg)
        {
            LogUpdateEventhandler c = new LogUpdateEventhandler(LogSetLastLine_impl);
            this.Invoke(c, msg);
        }

    }

    static class Program
    {
        public static myForm form;
        static System.IO.FileStream fstream;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {


            string GDAL_BASE_DIR = "C:\\Program Files (x86)\\GDAL";

            bool test = System.IO.Directory.Exists(GDAL_BASE_DIR);

            Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + GDAL_BASE_DIR);
            Environment.SetEnvironmentVariable("GDAL_DATA", GDAL_BASE_DIR + "\\gdal-data");
            Environment.SetEnvironmentVariable("GDAL_DRIVER_PATH", GDAL_BASE_DIR + "\\gdalplugins");
            Environment.SetEnvironmentVariable("PROJ_LIB", GDAL_BASE_DIR + "\\projlib");
            Environment.SetEnvironmentVariable("PYTHONPATH", GDAL_BASE_DIR + "\\python");

            Gdal.AllRegister();
            Ogr.RegisterAll();

            int i = Ogr.OGRERR_NONE;

            fstream = new System.IO.FileStream("GDBProxy_log.txt", FileMode.Create);
            LogPrintHandler logprint_impl = delegate(string msg)
            {
                byte[] b = new UTF8Encoding(true).GetBytes(msg + "\r\n");
                fstream.Write(b, 0, msg.Length + 2);
                form.LogUpdate(msg);
            };
            Log.setHandler(logprint_impl);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //MessageBox.Show("start");
            form = new Form1();
            Application.Run(form);

        }
    }
}
