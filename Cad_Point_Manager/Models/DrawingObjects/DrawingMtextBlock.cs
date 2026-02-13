using Cad_Point_Manager.Common;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using netDxf.Entities;
using SharpDX;
using Point = System.Windows.Point;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class DrawingMtextBlock : IDisposable
    {
        #region Fields
        private const float _mtextLineSpacingFactor = 0.15f;

        private readonly List<DrawingMtextRow> _rows = [];
        #endregion

        #region Properties
        public List<DrawingMtextRow> Rows => _rows;
        public float Height { get; set; } = 0;
        public float Width => _rows.Max(row => row.Width);
        public float MaxWidth { get; set; }
        public Vector3 BasePosition { get; set; } = Vector3.Zero;
        public float Rotation { get; set; } = 0;
        public int NumberOfRows => Rows.Count;
        public TextBox TextBox { get; set; } = TextBox.Empty;
        public Enums.TextAttachmentPoint AttachmentPoint { get; set; }
        public Vector3 AttachmentOffset { get; set; } = Vector3.Zero; // This will be used to offset the attachment point from the base position
        #endregion

        #region Constructors
        public DrawingMtextBlock(float maxWidth, Vector3 basePosition, MTextAttachmentPoint attachmentPoint, float rotation)
        {
            MaxWidth = maxWidth;
            BasePosition = basePosition;
            AttachmentPoint = TextRenderingHelpers.GetAttachmentPoint(attachmentPoint);
            Rotation = rotation;
        }
        #endregion

        #region Methods
        public void AddRow(DrawingMtextRow row)
        {
            _rows.Add(row);
        }
        public bool RemoveRow(DrawingMtextRow row)
        {
            return _rows.Remove(row);
        }
        public void ClearRows()
        {
            _rows.ForEach(row => row.Dispose());
            _rows.Clear();
            Height = 0;
            AttachmentOffset = Vector3.Zero;
        }

        public void SetTextPositions()
        {
            if (_rows.Count == 0) { return; } // No rows to set positions for

            SetRowBasePositions(); // Set the base positions for each 
            SetRowYOffsets(); // Calculate the Y offsets for each row based on line spacing
            SetRowXOffsets(); // Calculate the X offsets for each segment in the rows
            GetTextBox(Height);
            SetTextAttachmentOffsets(TextBox);

            foreach (var row in _rows)
            {
                row.ApplyTranslate(); // Apply the translation to each row's segments based on the current offsets
            }
        }

        public void SetTextAttachmentOffsets(TextBox textBox)
        {
            if (NumberOfRows > 0)
            {
                var firstRowHeight = _rows.First().Height; // Get the height of the first row to use as a reference for the attachment point calculation
                float width = (float)textBox.Width; // Get the width of the textbox to use for alignment calculations

                switch (AttachmentPoint)
                {
                    case Enums.TextAttachmentPoint.TopLeft:
                        {
                            AttachmentOffset = new(0, -firstRowHeight, 0);
                            break;
                        }
                    case Enums.TextAttachmentPoint.TopCenter:
                        {
                            AttachmentOffset = new(-width / 2, -firstRowHeight, 0);
                            break;
                        }
                    case Enums.TextAttachmentPoint.TopRight:
                        {
                            AttachmentOffset = new(-width, -firstRowHeight, 0);
                            break;
                        }
                    case Enums.TextAttachmentPoint.MiddleLeft:
                        {
                            if (_rows.Count > 1)
                            {
                                AttachmentOffset = new(0, firstRowHeight / 2, 0);  // For multiple rows, align to the middle of the first row instead of the top
                            }
                            else
                            {
                                AttachmentOffset = new(0, -firstRowHeight / 2, 0); // For single row, keep it centered vertically
                            }
                            break;
                        }
                    case Enums.TextAttachmentPoint.MiddleCenter:
                        {
                            if (_rows.Count > 1)
                            {
                                AttachmentOffset = new(-width / 2, firstRowHeight / 2, 0);  // For multiple rows, align to the middle of the first row instead of the top
                            }
                            else
                            {
                                AttachmentOffset = new(-width / 2, -firstRowHeight / 2, 0); // For single row, keep it centered vertically
                            }
                            break;
                        }
                    case Enums.TextAttachmentPoint.MiddleRight:
                        {
                            if (_rows.Count > 1)
                            {
                                AttachmentOffset = new(-width, firstRowHeight / 2, 0);  // For multiple rows, align to the middle of the first row instead of the top
                            }
                            else
                            {
                                AttachmentOffset = new(-width, -firstRowHeight / 2, 0); // For single row, keep it centered vertically
                            }
                            break;
                        }
                    case Enums.TextAttachmentPoint.BottomLeft:
                        {
                            if (_rows.Count > 1)
                            {
                                AttachmentOffset = new(0, firstRowHeight * 2, 0);  // For multiple rows, align to the middle of the first row instead of the top
                            }
                            else
                            {
                                AttachmentOffset = new(0, 0, 0); // For single row, keep it centered vertically
                            }
                            break;
                        }
                    case Enums.TextAttachmentPoint.BottomCenter:
                        {
                            if (_rows.Count > 1)
                            {
                                AttachmentOffset = new(-width / 2, firstRowHeight * 2, 0);  // For multiple rows, align to the middle of the first row instead of the top
                            }
                            else
                            {
                                AttachmentOffset = new(-width / 2, 0, 0); // For single row, keep it centered vertically
                            }
                            break;
                        }
                    case Enums.TextAttachmentPoint.BottomRight:
                        {
                            if (_rows.Count > 1)
                            {
                                AttachmentOffset = new(-width, firstRowHeight * 2, 0);  // For multiple rows, align to the middle of the first row instead of the top
                            }
                            else
                            {
                                AttachmentOffset = new(-width, 0, 0); // For single row, keep it centered vertically
                            }
                            break;
                        }
                    default:
                        break;
                }

                foreach (var row in _rows)
                {
                    if (row.TextAlignment == Enums.TextAlignment.Right)
                    {
                        row.UpdateTranslate(new Vector3((TextBox.Width - row.Width).ToFloat(), 0, 0));
                    }
                    if (row.TextAlignment == Enums.TextAlignment.Center)
                    {
                        row.UpdateTranslate(new Vector3((TextBox.Width - row.Width).ToFloat() / 2, 0, 0));
                    }
                    row.UpdateTranslate(AttachmentOffset);
                }
            }
        }
        public void GetTextBox(float textBoxHeight)
        {
            switch (AttachmentPoint)
            {
                case Enums.TextAttachmentPoint.TopLeft:
                    TextBox = new TextBox(new Point(BasePosition.X, BasePosition.Y), new Point(BasePosition.X, BasePosition.Y),
                        new Point(BasePosition.X + MaxWidth, BasePosition.Y - textBoxHeight));
                    break;

                case Enums.TextAttachmentPoint.TopCenter:
                    TextBox = new TextBox(new Point(BasePosition.X, BasePosition.Y), new Point(BasePosition.X - MaxWidth * 0.5f, BasePosition.Y),
                        new Point(BasePosition.X + MaxWidth * 0.5f, BasePosition.Y - textBoxHeight));
                    break;

                case Enums.TextAttachmentPoint.TopRight:
                    TextBox = new TextBox(new Point(BasePosition.X, BasePosition.Y), new Point(BasePosition.X - MaxWidth, BasePosition.Y),
                        new Point(BasePosition.X, BasePosition.Y - textBoxHeight));
                    break;

                case Enums.TextAttachmentPoint.MiddleLeft:
                    TextBox = new TextBox(new Point(BasePosition.X, BasePosition.Y), new Point(BasePosition.X, BasePosition.Y + textBoxHeight * 0.5f),
                        new Point(BasePosition.X + MaxWidth, BasePosition.Y - textBoxHeight * 0.5f));
                    break;

                case Enums.TextAttachmentPoint.MiddleCenter:
                    TextBox = new TextBox(new Point(BasePosition.X, BasePosition.Y), new Point(BasePosition.X - MaxWidth * 0.5f, BasePosition.Y + textBoxHeight * 0.5f),
                        new Point(BasePosition.X + MaxWidth * 0.5f, BasePosition.Y - textBoxHeight * 0.5f));
                    break;

                case Enums.TextAttachmentPoint.MiddleRight:
                    TextBox = new TextBox(new Point(BasePosition.X, BasePosition.Y), new Point(BasePosition.X - MaxWidth, BasePosition.Y + textBoxHeight * 0.5f),
                        new Point(BasePosition.X, BasePosition.Y - textBoxHeight * 0.5f));
                    break;

                case Enums.TextAttachmentPoint.BottomLeft:
                    TextBox = new TextBox(new Point(BasePosition.X, BasePosition.Y), new Point(BasePosition.X, BasePosition.Y + textBoxHeight),
                        new Point(BasePosition.X + MaxWidth, BasePosition.Y));
                    break;

                case Enums.TextAttachmentPoint.BottomCenter:
                    TextBox = new TextBox(new Point(BasePosition.X, BasePosition.Y), new Point(BasePosition.X - MaxWidth * 0.5f, BasePosition.Y + textBoxHeight),
                        new Point(BasePosition.X + MaxWidth * 0.5f, BasePosition.Y));
                    break;

                case Enums.TextAttachmentPoint.BottomRight:
                    TextBox = new TextBox(new Point(BasePosition.X, BasePosition.Y), new Point(BasePosition.X - MaxWidth, BasePosition.Y + textBoxHeight),
                        new Point(BasePosition.X, BasePosition.Y));
                    break;

                default:
                    break;
            }
        }

        private void SetRowBasePositions()
        {
            foreach (var row in _rows)
            {
                row.UpdateTranslate(BasePosition);
                row.UpdateTranslate(AttachmentOffset);
            }
        }
        private void SetRowYOffsets()
        {
            float currentYOffset = 0;
            Height = 0;
            DrawingMtextRow currentRow;
            DrawingMtextRow prevRow;

            if (_rows.Count == 1)
            {
                currentRow = _rows[0];
                float y = currentRow.Height + 2 * (currentRow.Height * _mtextLineSpacingFactor);
                Height += y;

                return;
            }

            for (int i = 0; i < _rows.Count; i++)
            {
                if (i == 0)
                {
                    currentRow = _rows[i];
                    float y = currentRow.Height + (currentRow.Height * _mtextLineSpacingFactor);
                    Height += y;

                    continue;
                }

                currentRow = _rows[i];
                prevRow = _rows[i - 1];

                if (i - 1 == _rows.Count)
                {
                    float y = currentRow.Height + (currentRow.Height * _mtextLineSpacingFactor) + (prevRow.Height * _mtextLineSpacingFactor);
                    currentYOffset -= y;
                    Height += y;
                }
                else
                {
                    float y = currentRow.Height + 2 * (currentRow.Height * _mtextLineSpacingFactor) + 2 * (prevRow.Height * _mtextLineSpacingFactor);

                    currentYOffset -= y;
                    Height += y;
                }

                currentRow.UpdateTranslate(new Vector3(0, currentYOffset, 0));
            }
        }

        private void SetRowXOffsets()
        {
            foreach (var row in _rows)
            {
                row.SetTextSegmentsXOffset();
            }
        }

        public void AddSegment(DrawingMtextSegment segment)
        {
            if (NumberOfRows == 0)
            {
                DrawingMtextRow newRow = new([segment], BasePosition, MaxWidth);
                newRow.GetHeight();
                AddRow(newRow);
            }
            else
            {
                var lastRow = _rows.Last();

                if (lastRow.Width + segment.SpaceWidth + segment.Bounds.Width > MaxWidth || segment.IsNewLine)
                {
                    DrawingMtextRow newRow = new([segment], BasePosition, MaxWidth);
                    newRow.GetHeight();
                    AddRow(newRow);
                }
                else
                {
                    lastRow.AddSegment(segment);
                    lastRow.GetHeight();
                }
            }
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
                    foreach (var row in _rows)
                    {
                        row.Dispose();
                    }
                    ClearRows();
                }

                disposedValue = true;
            }
        }

        // Override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~DrawingMtextBlock()
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
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
