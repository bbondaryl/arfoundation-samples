using System;
using System.Text;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace UnityEngine.XR.ARFoundation.Samples
{
    /// <summary>
    /// This component tests getting the latest camera image
    /// and converting it to RGBA format. If successful,
    /// it displays the image on the screen as a RawImage
    /// and also displays information about the image.
    ///
    /// This is useful for computer vision applications where
    /// you need to access the raw pixels from camera image
    /// on the CPU.
    ///
    /// This is different from the ARCameraBackground component, which
    /// efficiently displays the camera image on the screen. If you
    /// just want to blit the camera texture to the screen, use
    /// the ARCameraBackground, or use Graphics.Blit to create
    /// a GPU-friendly RenderTexture.
    ///
    /// In this example, we get the camera image data on the CPU,
    /// convert it to an RGBA format, then display it on the screen
    /// as a RawImage texture to demonstrate it is working.
    /// This is done as an example; do not use this technique simply
    /// to render the camera image on screen.
    /// </summary>
    public class TestDepthImage : MonoBehaviour
    {
        /// <summary>
        /// Name of the texture rotation property in the shader.
        /// </summary>
        const string k_TextureRotationName = "_TextureRotation";

        /// <summary>
        /// ID of the texture rotation property in the shader.
        /// </summary>
        static readonly int k_TextureRotationId = Shader.PropertyToID(k_TextureRotationName);

        /// <summary>
        /// A string builder for construction of strings.
        /// </summary>
        readonly StringBuilder m_StringBuilder = new StringBuilder();

        [SerializeField]
        [Tooltip("The AROcclusionManager which will produce frame events.")]
        AROcclusionManager m_OcclusionManager;

        /// <summary>
        /// Get or set the <c>AROcclusionManager</c>.
        /// </summary>
        public AROcclusionManager occlusionManager
        {
            get { return m_OcclusionManager; }
            set { m_OcclusionManager = value; }
        }

        [SerializeField]
        RawImage m_RawImage;

        /// <summary>
        /// The UI RawImage used to display the image on screen.
        /// </summary>
        public RawImage rawImage
        {
            get { return m_RawImage; }
            set { m_RawImage = value; }
        }

        [SerializeField]
        Text m_ImageInfo;

        /// <summary>
        /// The UI Text used to display information about the image on screen.
        /// </summary>
        public Text imageInfo
        {
            get { return m_ImageInfo; }
            set { m_ImageInfo = value; }
        }

        /// <summary>
        /// The current screen orientation remembered so that we are only updating the raw image layout when it changes.
        /// </summary>
        ScreenOrientation m_CurrentScreenOrientation;

        /// <summary>
        /// The current texture aspect ratio remembered so that we can resize the raw image layout when it changes.
        float m_TextureAspectRatio = 1.0f;

        void OnEnable()
        {
            m_CurrentScreenOrientation = Screen.orientation;
            LayoutRawImage();
        }

        void Update()
        {
            Debug.Assert(m_OcclusionManager != null, "no occlusion manager");
            if ((m_OcclusionManager.descriptor?.supportsHumanSegmentationStencilImage == false)
                && (m_OcclusionManager.descriptor?.supportsHumanSegmentationDepthImage == false)
                && (m_OcclusionManager.descriptor?.supportsEnvironmentDepthImage == false))
            {
                if (m_ImageInfo != null)
                {
                    m_ImageInfo.text = "Depth functionality is not supported on this device.";
                }
                return;
            }

            Texture2D humanStencil = m_OcclusionManager.humanStencilTexture;
            Texture2D humanDepth = m_OcclusionManager.humanDepthTexture;
            Texture2D envDepth = m_OcclusionManager.environmentDepthTexture;

            Texture2D displayTexture = envDepth;

            m_StringBuilder.Clear();
            LogTextureInfo(m_StringBuilder, "stencil", humanStencil);
            LogTextureInfo(m_StringBuilder, "depth", humanDepth);
            LogTextureInfo(m_StringBuilder, "env", envDepth);
            if (m_ImageInfo != null)
            {
                m_ImageInfo.text = m_StringBuilder.ToString();
            }
            else
            {
                Debug.Log(m_StringBuilder.ToString());
            }

            Debug.Assert(m_RawImage != null, "no raw image");
            m_RawImage.texture = displayTexture;

            float textureAspectRatio = (displayTexture == null) ? 1.0f : ((float)displayTexture.width / (float)displayTexture.height);

            if ((m_CurrentScreenOrientation != Screen.orientation)
                || !Mathf.Approximately(m_TextureAspectRatio, textureAspectRatio))
            {
                m_CurrentScreenOrientation = Screen.orientation;
                m_TextureAspectRatio = textureAspectRatio;
                LayoutRawImage();
            }
        }

        void LogTextureInfo(StringBuilder stringBuilder, string textureName, Texture2D texture)
        {
            stringBuilder.AppendFormat("texture : {0}\n", textureName);
            if (texture == null)
            {
                stringBuilder.AppendFormat("   <null>\n");
            }
            else
            {
                stringBuilder.AppendFormat("   format : {0}\n", texture.format.ToString());
                stringBuilder.AppendFormat("   width  : {0}\n", texture.width);
                stringBuilder.AppendFormat("   height : {0}\n", texture.height);
                stringBuilder.AppendFormat("   mipmap : {0}\n", texture.mipmapCount);
            }
        }

        void LayoutRawImage()
        {
            Debug.Assert(m_RawImage != null, "no raw image");

            float minDimension = 480.0f;
            float maxDimension = Mathf.Round(minDimension * m_TextureAspectRatio);
            Vector2 rectSize;
            float rotation;

            switch (m_CurrentScreenOrientation)
            {
                case ScreenOrientation.LandscapeRight:
                    rectSize = new Vector2(maxDimension, minDimension);
                    rotation = 0.0f;
                    break;
                case ScreenOrientation.LandscapeLeft:
                    rectSize = new Vector2(maxDimension, minDimension);
                    rotation = 180.0f;
                    break;
                case ScreenOrientation.PortraitUpsideDown:
                    rectSize = new Vector2(minDimension, maxDimension);
                    rotation = 270.0f;
                    break;
                case ScreenOrientation.Portrait:
                default:
                    rectSize = new Vector2(minDimension, maxDimension);
                    rotation = 90.0f;
                    break;
            }
            m_RawImage.rectTransform.sizeDelta = rectSize;
            m_RawImage.material.SetFloat(k_TextureRotationId, rotation);
        }
    }
}
