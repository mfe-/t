using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t.App.Controls
{
    public class CardCollectionView : CollectionView
    {
        public override SizeRequest Measure(double widthConstraint, double heightConstraint, MeasureFlags flags = MeasureFlags.None)
        {
            var sizeRequest = base.Measure(widthConstraint, heightConstraint, flags);
            return sizeRequest;
        }
        protected override void OnChildAdded(Element child)
        {
            base.OnChildAdded(child);
        }
         
    }
}
