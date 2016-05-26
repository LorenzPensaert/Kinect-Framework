using Microsoft.Kinect;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KinectSDK.Logic {

    internal static class KinectUtils {

        /// <summary>
        /// Gets the angle between 2 Joints
        /// </summary>
        /// <param name="first">First joint</param>
        /// <param name="second">Second joint</param>
        /// <returns>The angle</returns>
        public static double AngleBetween (Joint first, Joint second) {
            //ArcTangens(Y2 - Y1, X2 - X1) = Radians * 180 / PI => z Degrees
            return Math.Atan2(second.Position.Y - first.Position.Y, second.Position.X - first.Position.X) * 180 / Math.PI;
        }

        /// <summary>
        /// Gets the angle on the Y-axis between 2 joints
        /// </summary>
        /// <param name="first">First joint</param>
        /// <param name="second">Second joint</param>
        /// <returns>The angle</returns>
        public static double YAngleBetween (Joint first, Joint second) {
            return Math.Atan(first.Position.Z);
        }

        /// <summary>
        /// Gets the distance between 2 joints
        /// </summary>
        /// <param name="first">First joint</param>
        /// <param name="second">Second joint</param>
        /// <returns>The distance</returns>
        public static double DistanceBetween (Joint first, Joint second) {
            //Sqrt((x1 - x2)^2 + (y1 - y2)^2 + (z1 - z2)^2)
            return Math.Sqrt(Math.Pow((first.Position.X - second.Position.X), 2) + Math.Pow((first.Position.Y - second.Position.Y), 2) + Math.Pow((first.Position.Z - second.Position.Z), 2));
        }

        /// <summary>
        /// Gets the distance to the Kinect origin from a joint
        /// </summary>
        /// <param name="first">The first joint</param>
        /// <returns></returns>
        public static double DistanceOrigin (Joint first) {
            return Math.Sqrt(Math.Pow(first.Position.X, 2) + Math.Pow(first.Position.Y, 2) + Math.Pow(first.Position.Z, 2));
        }

        /// <summary>
        /// Translates a Color frame to an imagesource
        /// </summary>
        /// <param name="frame">Color frame</param>
        /// <returns>ImageSource</returns>
        public static ImageSource ToBitmap (ColorFrame frame) {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;

            byte[] pixels = new byte[width * height * ((PixelFormats.Bgr32.BitsPerPixel + 7) / 8)];

            if (frame.RawColorImageFormat == ColorImageFormat.Bgra) {
                frame.CopyRawFrameDataToArray(pixels);
            } else {
                frame.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Bgra);
            }

            int stride = width * PixelFormats.Bgr32.BitsPerPixel / 8;

            return BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgr32, null, pixels, stride);
        }

        /// <summary>
        /// Translates a Depth frame to an imagesource
        /// </summary>
        /// <param name="frame">Depth frame</param>
        /// <returns>ImageSource</returns>
        public static ImageSource ToBitmap (DepthFrame frame) {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;

            ushort minDepth = frame.DepthMinReliableDistance;
            ushort maxDepth = frame.DepthMaxReliableDistance;

            ushort[] depthData = new ushort[width * height];
            byte[] pixelData = new byte[width * height * (PixelFormats.Bgr32.BitsPerPixel + 7) / 8];

            frame.CopyFrameDataToArray(depthData);

            int colorIndex = 0;
            for (int depthIndex = 0; depthIndex < depthData.Length; ++depthIndex) {
                ushort depth = depthData[depthIndex];
                byte intensity = (byte) (depth >= minDepth && depth <= maxDepth ? depth : 0);

                pixelData[colorIndex++] = intensity; // Blue
                pixelData[colorIndex++] = intensity; // Green
                pixelData[colorIndex++] = intensity; // Red

                ++colorIndex;
            }

            int stride = width * PixelFormats.Bgr32.BitsPerPixel / 8;

            return BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgr32, null, pixelData, stride);
        }

        /// <summary>
        /// Translates a Infrared frame to an imagesource
        /// </summary>
        /// <param name="frame">Infrared frame</param>
        /// <returns>ImageSource</returns>
        public static ImageSource ToBitmap (InfraredFrame frame) {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;

            ushort[] infraredData = new ushort[width * height];
            byte[] pixelData = new byte[width * height * (PixelFormats.Bgr32.BitsPerPixel + 7) / 8];

            frame.CopyFrameDataToArray(infraredData);

            int colorIndex = 0;
            for (int infraredIndex = 0; infraredIndex < infraredData.Length; ++infraredIndex) {
                ushort ir = infraredData[infraredIndex];
                byte intensity = (byte) (ir >> 8);

                pixelData[colorIndex++] = intensity; // Blue
                pixelData[colorIndex++] = intensity; // Green
                pixelData[colorIndex++] = intensity; // Red

                ++colorIndex;
            }

            int stride = width * PixelFormats.Bgr32.BitsPerPixel / 8;

            return BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgr32, null, pixelData, stride);
        }

        /// <summary>
        /// Scales a joint to a 2D perspective
        /// </summary>
        /// <param name="joint">The joint</param>
        /// <param name="width">The width of the canvas</param>
        /// <param name="height">The height of the canvas</param>
        /// <param name="skeletonMaxX">The skeleton' max X</param>
        /// <param name="skeletonMaxY">The skeleton' max Y</param>
        /// <returns>The scaled joint</returns>
        public static Joint ScaleTo (this Joint joint, double width, double height, float skeletonMaxX, float skeletonMaxY) {
            joint.Position = new CameraSpacePoint {
                X = Scale(width, skeletonMaxX, joint.Position.X),       // Width = Canvas.ActualWidth
                Y = Scale(height, skeletonMaxY, -joint.Position.Y),     // Height = Canvas.ActualHeight
                Z = joint.Position.Z
            };

            return joint;
        }

        /// <summary>
        /// Scales a joint to a 2D perspective
        /// </summary>
        /// <param name="joint">The joint</param>
        /// <param name="width">The width of the canvas</param>
        /// <param name="height">The height of the canvas</param>
        /// <returns>The scaled joint</returns>
        public static Joint ScaleTo (this Joint joint, double width, double height) {
            return ScaleTo(joint, width, height, 1.0f, 1.0f);
        }

        /// <summary>
        /// Scales a position to a max position/max skeleton position
        /// </summary>
        /// <param name="maxPixel">The max pixel</param>
        /// <param name="maxSkeleton">The max skeleton position</param>
        /// <param name="position">The position</param>
        /// <returns>The new position</returns>
        private static float Scale (double maxPixel, double maxSkeleton, float position) {
            float value = (float) ((((maxPixel / maxSkeleton) / 2) * position) + (maxPixel / 2));

            if (value > maxPixel) {
                return (float) maxPixel;
            }

            if (value < 0) {
                return 0;
            }

            return value;
        }

        /// <summary>
        /// Gets the closest body in a body frame
        /// </summary>
        /// <param name="frame">The body frame</param>
        /// <returns>The closest body in the frame</returns>
        public static Body GetClosestBody (BodyFrame frame) {
            Body result = null;
            double closestBodyDistance = double.MaxValue;

            Body[] bodies = new Body[frame.BodyCount];
            frame.GetAndRefreshBodyData(bodies);

            foreach (var body in bodies) {
                if (body.IsTracked) {
                    var currentJoint = body.Joints[JointType.SpineBase];
                    var currentDistance = DistanceOrigin(currentJoint);

                    if (result == null || currentDistance < closestBodyDistance) {
                        result = body;
                        closestBodyDistance = currentDistance;
                    }
                }
            }

            return result;
        }
    }
}