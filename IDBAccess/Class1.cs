using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HassanS;

namespace HassanS
{
    public delegate void LogPrintHandler(string msg);

    public static class Log
    {
        static LogPrintHandler handler;

        public static void Print(string msg)
        {
            Log.handler(msg);

        }

        public static void setHandler(LogPrintHandler handler)
        {
            Log.handler = handler;
        }

    }

    public enum GeoObjectType
    {
        UNKNOWN = 0,
        PROJECT_OUTLINE_POLYGON,
        SEISMIC_2D_LINE,
        SEISMIC_3D_POLYGON,
        WELL_POINT,
        WELL_TRAJECTORY
    };

    public interface IDBAccess
    {
        
        void Initialize(string GDBpath, bool append);
        bool IsInitialized();

        void FormLogPrint(string msg);

        void FormLogSet(string msg);

        void FormLogSetLastLine(string msg);

        //object add_seismic2D_line_feature(string name, string survey_name, string feature_path_inside_project, Polygon shape, object project_OID );

        //object add_seismic3D_feature(string survey_name, string feature_path_inside_project, Polygon shape, object project_OID);

        //object add_well_feature(
        //    string well_name,
        //    string well_UWI,
        //    double MDDF, 
        //    double TVDDF, 
        //    double TVDSS, 
        //    double KB,
        //    string unitName,
        //    double XW84,
        //    double YW84,
        //    double X,
        //    double Y,
        //    string WKT,
        //    string feature_path_inside_project, Point shape, object project_OID);

        object add_Project(string project_name, string project_dir_path, string country_name, string CRS_WKT, string EarlyBoundTransform_WKT );

        object get_Project_by_path(string project_dir_path);

        object finalize_Project(object project_OID);

    }

    
}
