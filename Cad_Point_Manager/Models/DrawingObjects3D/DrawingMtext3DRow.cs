using Cad_Point_Manager.Common;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public class DrawingMtext3DRow : IDisposable
    {
        #region Properties
        public float Height { get; set; } = 0;
        public float MaxWidth { get; set; } 
        public List<DrawingMtextSegment3D> Segments { get; set; } = [];
        public Enums.TextAlignment TextAlignment { get; set; } = Enums.TextAlignment.Left;
        public Vector3 BaseRowPosition { get; set; } = Vector3.Zero;
        
        public Vector3 CurrentTranslate { get; private set; } = Vector3.Zero;
        public Vector3 OverallTranslate { get; private set; } = Vector3.Zero;

        public float Width => (float)Segments.Sum(segment => segment.Bounds.Width);
        #endregion

        #region Constructors
        public DrawingMtext3DRow(float maxWidth) { MaxWidth = maxWidth; }

        public DrawingMtext3DRow(List<DrawingMtextSegment3D> segments, Enums.TextAlignment textAlignment, Vector3 basePosition, float maxWidth)
        {
            Segments = segments;
            TextAlignment = textAlignment;
            BaseRowPosition = basePosition;
            MaxWidth = maxWidth;
        }

        public DrawingMtext3DRow(Enums.TextAlignment textAlignment, Vector3 basePosition, float maxWidth)
        {
            TextAlignment = textAlignment;
            BaseRowPosition = basePosition;
            MaxWidth = maxWidth;
        }
        #endregion

        #region Methods
        public void AddSegment(DrawingMtextSegment3D segment)
        {
            Segments.Add(segment);
            float rowHeight = segment.FontHeight;

            if (rowHeight > Height) { Height = rowHeight; }
        }

        public void GetHeight() 
        {
            for (int i = 0; i < Segments.Count; i++)
            {
                var segment = Segments[i];
                //float rowHeight = (float)(segment.FontHeight + segment.FontHeight * _mtextLineSpacingFactor * 
                //    segment.DrawingMtext3D.DxfMtext.LineSpacingFactor);
                float rowHeight = segment.FontHeight;

                if (rowHeight > Height) { Height = rowHeight; }
            }
        }
        public void SetTextSegmentsXOffset()
        {
            DrawingMtextSegment3D prevSegment = null;
            float currentXOffset = 0;
            for (int i = 0; i < Segments.Count; i++)
            {
                var segment = Segments[i];

                var prevSpaceWidth = prevSegment?.SpaceWidth ?? 0;
                var prevSegmentWidth = (float)(prevSegment?.Bounds.Width ?? 0);
                if (i != 0)
                {
                    //currentXOffset += prevSegmentWidth + prevSpaceWidth * 0.5f + segment.SpaceWidth * 0.5f;
                    currentXOffset += prevSegmentWidth + segment.SpaceWidth;
                }
                segment.RowXOffset = currentXOffset;

                prevSegment = segment;
            }
        }

        public void UpdateTranslate(Vector3 offset)
        {
            CurrentTranslate += offset;
            OverallTranslate += offset;
        }
        public void ApplyTranslate()
        {
            foreach (var segment in Segments)
            {
                segment.ApplyTranslate(CurrentTranslate);
            }
            CurrentTranslate = Vector3.Zero; // Reset current translate after applying to segments
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Segments.ForEach(s => s.Dispose());
                    Segments.Clear();
                }

                disposedValue = true;
            }
        }

        // Override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~DrawingMtext3DRow()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // Uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
