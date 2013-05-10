using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace QuickAnimator
{
    [XmlRoot("AnimatorSave")]
    public class AnimatorSave
    {
        [XmlElement("CantidadFrames")]
        public int CantidadFrames { get; set; }
    }
}
