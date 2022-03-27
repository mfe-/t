//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace t.App.Controls.Themes
//{
//    public partial class Generic : ResourceDictionary
//    {
//        private static bool registered;

//        public Generic()
//        {
//            EnsureRegistered(this);
//        }

//        internal static void EnsureRegistered(ResourceDictionary resourceDictionary)
//        {
//            // don't do extra work
//            if (registered)
//                return;

//            // get the dictionary if we can
//            var merged = Application.Current?.Resources?.MergedDictionaries;
//            if (merged != null)
//            {
//                // check to see if we are added already
//                foreach (var dic in merged)
//                {
//                    if (dic.GetType() == typeof(Generic))
//                    {
//                        registered = true;
//                        break;
//                    }
//                }

//                // if we are not added, add ourselves
//                if (!registered)
//                {
//                    merged.Add(resourceDictionary);
//                    registered = true;
//                }
//            }
//        }
//    }
//}
