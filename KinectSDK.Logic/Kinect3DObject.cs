using System.Windows.Media.Media3D;

namespace KinectSDK.Logic {

    public class Kinect3DObject {

        /// <summary>
        /// The First Joint
        /// </summary>
        public int FirstJoint { get; set; }

        /// <summary>
        /// The Offset X
        /// </summary>
        public double OffsetX { get; set; } = 0;

        /// <summary>
        /// The Offset Y
        /// </summary>
        public double OffsetY { get; set; } = 0;

        /// <summary>
        /// The Offset Z
        /// </summary>
        public double OffsetZ { get; set; } = 0;

        /// <summary>
        /// Is the 3D model moveable based on the body
        /// </summary>
        public bool IsMoveable { get; set; } = true;

        /// <summary>
        /// Is the 3D model rotateable based on the body
        /// </summary>
        public bool IsRotateable { get; set; } = true;

        /// <summary>
        /// The 3D model to be rendered
        /// </summary>
        public Model3D Model { get; set; }

        /// <summary>
        /// The constructor for the Kinect 3D Object
        /// </summary>
        /// <param name="firstJoint">The joint where the Model should be placed on</param>
        /// <param name="model">The 3D Model</param>
        public Kinect3DObject (KinectJoint firstJoint, Model3D model) {
            FirstJoint = (int) firstJoint;
            Model = model;
        }
    }
}