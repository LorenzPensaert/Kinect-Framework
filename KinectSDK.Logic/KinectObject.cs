using Microsoft.Kinect;
using System;
using System.Windows.Media;

namespace KinectSDK.Logic {

    /// <summary>
    /// Kinect Object (2D Images)
    /// </summary>
    public class KinectObject {

        /// <summary>
        /// The first joint
        /// </summary>
        public int FirstJoint { get; set; }

        /// <summary>
        /// The second joint
        /// </summary>
        public int SecondJoint { get; set; }

        /// <summary>
        /// The Offset X
        /// </summary>
        public int OffsetX { get; set; } = 0;

        /// <summary>
        /// The Offset Y
        /// </summary>
        public int OffsetY { get; set; } = 0;

        /// <summary>
        /// The Image to be drawn
        /// </summary>
        public ImageSource Image { get; set; }

        /// <summary>
        /// The Kinect Object constructor
        /// </summary>
        /// <param name="firstJoint">The first joint</param>
        /// <param name="secondJoint">The second joint</param>
        /// <param name="image">The image to be drawn</param>
        /// <param name="offsetX">The offset X</param>
        /// <param name="offsetY">The offset Y</param>
        public KinectObject (KinectJoint firstJoint, KinectJoint secondJoint, ImageSource image, int offsetX = 0, int offsetY = 0) {
            this.FirstJoint = (int) firstJoint;
            this.SecondJoint = (int) secondJoint;
            this.Image = image;
            if (offsetX != 0) OffsetX = offsetX;
            if (offsetY != 0) OffsetY = offsetY;
        }
    }
}