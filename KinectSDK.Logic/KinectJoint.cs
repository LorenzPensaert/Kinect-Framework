using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectSDK.Logic {

    /// <summary>
    /// The joints where objects can be attached
    /// </summary>
    public enum KinectJoint {
        AnkleLeft = 14,
        AnkleRight = 18,
        ElbowLeft = 5,
        ElbowRight = 9,
        FootLeft = 15,
        FootRight = 19,
        HandLeft = 7,
        HandRight = 11,
        HandTipLeft = 21,
        HandTipRight = 23,
        Head = 3,
        HipLeft = 12,
        HipRight = 16,
        KneeLeft = 13,
        KneeRight = 17,
        Neck = 2,
        ShoulderLeft = 4,
        ShoulderRight = 8,
        SpineBase = 0,
        SpineMid = 1,
        SpineShoulder = 20,
        ThumbLeft = 22,
        ThumbRight = 24,
        WristLeft = 6,
        WristRight = 10
    }
}