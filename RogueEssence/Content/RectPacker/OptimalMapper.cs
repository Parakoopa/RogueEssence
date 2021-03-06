﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace RectPacker
{
    /// <summary>
    /// An IMapper takes a series of images, and figures out how these could be combined in a sprite.
    /// It returns the dimensions that the sprite will have, and the locations of each image within that sprite.
    /// 
    /// This object does not create the sprite image itself. It only figures out how it needs to be constructed.
    /// </summary>
    public class OptimalMapper
    {
        private Canvas _canvas = null;

        protected Canvas Canvas { get { return _canvas; } }

        protected float CutoffEfficiency { get; private set; }
        protected int MaxNbrCandidateSprites { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="canvas">Canvas object to use to map the images</param>
        /// <param name="cutoffEfficiency">
        /// When the object's Mapping method produces a candidate sprite with this efficiency or higher, it stops
        /// trying to get a better one and returns the sprite.
        /// 
        /// Set this to for example 0.9 if that efficiency is good enough for you. 
        /// Set to 1.0 or higher if you want the most efficient sprite.
        /// </param>
        /// <param name="maxNbrCandidateSprites">
        /// The maximum number of candidate sprites that will be generated by the Mapping method.
        /// Set this to 1 if you want it to return the very first sprite that it manages to generate.
        /// If you want it to try to generate no more than 2 sprites before returning the best one, set this to 2, etc.
        /// Set to Int32.MaxValue if you don't want any restrictions on the number of candidate sprites generated.
        /// 
        /// If you set cutoff Efficiency to less than 1, and maxNbrCandidateSprites to less than Int32.MaxValue,
        /// the Mapper method will stop trying to get a better sprite the moment it hits one of these limitations.
        /// </param>
        public OptimalMapper(Canvas canvas, float cutoffEfficiency, int maxNbrCandidateSprites)
        {
            _canvas = canvas;
            CutoffEfficiency = cutoffEfficiency;
            MaxNbrCandidateSprites = maxNbrCandidateSprites;
        }

        public OptimalMapper(Canvas canvas) : this(canvas, 1.0f, Int32.MaxValue)
        {
        }

        /// <summary>
        /// Works out how to map a series of images into a sprite.
        /// </summary>
        /// <param name="images">
        /// The list of images to place into the sprite.
        /// </param>
        /// <returns>
        /// A SpriteInfo object. This describes the locations of the images within the sprite,
        /// and the dimensions of the sprite.
        /// </returns>
        public Atlas Mapping(IEnumerable<ImageInfo> images)
        {
            int candidateSpritesGenerated = 0;
            int canvasRectangleAddAttempts = 0;
            int canvasNbrCellsGenerated = 0;

            // Sort the images by height descending
            IOrderedEnumerable<ImageInfo> imageInfosHighestFirst =
                images.OrderByDescending(p => p.Height);

            int totalAreaAllImages =
                (from a in imageInfosHighestFirst select a.Width * a.Height).Sum();

            int widthWidestImage =
                (from a in imageInfosHighestFirst select a.Width).Max();

            int heightHighestImage = imageInfosHighestFirst.First().Height;

            Atlas bestSprite = null;

            int canvasMaxWidth = 4096;
            int canvasMaxHeight = Math.Max(heightHighestImage, (int)Math.Ceiling((float)totalAreaAllImages / canvasMaxWidth));

            while (canvasMaxWidth >= widthWidestImage)
            {
                CanvasStats canvasStats = new CanvasStats();
                int lowestFreeHeightDeficitTallestRightFlushedImage;
                Atlas spriteInfo =
                    MappingRestrictedBox(imageInfosHighestFirst, canvasMaxWidth, canvasMaxHeight, canvasStats, out lowestFreeHeightDeficitTallestRightFlushedImage);

                canvasRectangleAddAttempts += canvasStats.RectangleAddAttempts;
                canvasNbrCellsGenerated += canvasStats.NbrCellsGenerated;

                if (spriteInfo == null)
                {
                    // Failure - Couldn't generate a SpriteInfo with the given maximum canvas dimensions

                    // Try again with a greater max height. Add enough height so that 
                    // you don't get the same rectangle placement as this time.

                    if (canvasStats.LowestFreeHeightDeficit == Int32.MaxValue)
                    {
                        canvasMaxHeight++;
                    }
                    else
                    {
                        canvasMaxHeight += canvasStats.LowestFreeHeightDeficit;
                    }
                }
                else
                {
                    // Success - Managed to generate a SpriteInfo with the given maximum canvas dimensions

                    candidateSpritesGenerated++;

                    // Find out if the new SpriteInfo is better than the current best one
                    if ((bestSprite == null) || (bestSprite.Area > spriteInfo.Area))
                    {
                        bestSprite = spriteInfo;

                        float bestEfficiency = (float)totalAreaAllImages / spriteInfo.Area;
                        if (bestEfficiency >= CutoffEfficiency)
                            break;
                    }

                    if (candidateSpritesGenerated >= MaxNbrCandidateSprites)
                        break; 

                    // Try again with a reduce maximum canvas width, to see if we can squeeze out a smaller sprite
                    // Note that in this algorithm, the maximum canvas width is never increased, so a new sprite
                    // always has the same or a lower width than an older sprite.
                    canvasMaxWidth = bestSprite.Width - 1;

                    // Now that we've decreased the width of the canvas to 1 pixel less than the width
                    // taken by the images on the canvas, we know for sure that the images whose
                    // right borders are most to the right will have to move up.
                    //
                    // To make sure that the next try is not automatically a failure, increase the height of the 
                    // canvas sufficiently for the tallest right flushed image to be placed. Note that when
                    // images are placed sorted by highest first, it will be the tallest right flushed image
                    // that will fail to be placed if we don't increase the height of the canvas sufficiently.

                    if (lowestFreeHeightDeficitTallestRightFlushedImage == Int32.MaxValue)
                    {
                        canvasMaxHeight++;
                    }
                    else
                    {
                        canvasMaxHeight += lowestFreeHeightDeficitTallestRightFlushedImage;
                    }
                }

                // ---------------------
                // Adjust max canvas width and height to cut out sprites that we'll never accept
                if (bestSprite != null)
                {
                    int bestSpriteArea = bestSprite.Area;
                    bool candidateBiggerThanBestSprite;
                    bool candidateSmallerThanCombinedImages;

                    while (
                        (canvasMaxWidth >= widthWidestImage) &&
                        (!CandidateCanvasFeasable(
                                canvasMaxWidth, canvasMaxHeight, bestSpriteArea, totalAreaAllImages,
                                out candidateBiggerThanBestSprite, out candidateSmallerThanCombinedImages)))
                    {
                        if (candidateBiggerThanBestSprite) { canvasMaxWidth--; }
                        if (candidateSmallerThanCombinedImages) { canvasMaxHeight++; }
                    }
                }
            }

            return bestSprite;
        }


        /// <summary>
        /// Works out whether there is any point in trying to fit the images on a canvas
        /// with the given width and height.
        /// </summary>
        /// <param name="canvasMaxWidth">Candidate canvas width</param>
        /// <param name="canvasMaxHeight">Candidate canvas height</param>
        /// <param name="bestSpriteArea">Area of the smallest sprite produces so far</param>
        /// <param name="totalAreaAllImages">Total area of all images</param>
        /// <param name="candidateBiggerThanBestSprite">true if the candidate canvas is bigger than the best sprite so far</param>
        /// <param name="candidateSmallerThanCombinedImages">true if the candidate canvas is smaller than the combined images</param>
        /// <returns></returns>
        protected virtual bool CandidateCanvasFeasable(
            int canvasMaxWidth, int canvasMaxHeight, int bestSpriteArea, int totalAreaAllImages,
            out bool candidateBiggerThanBestSprite, out bool candidateSmallerThanCombinedImages)
        {
            int candidateArea = canvasMaxWidth * canvasMaxHeight;
            candidateBiggerThanBestSprite = (candidateArea > bestSpriteArea);
            candidateSmallerThanCombinedImages = (candidateArea < totalAreaAllImages);

            return !(candidateBiggerThanBestSprite || candidateSmallerThanCombinedImages);
        }


        /// <summary>
        /// Produces a mapping to a sprite that has given maximum dimensions.
        /// If the mapping can not be done inside those dimensions, returns null.
        /// </summary>
        /// <param name="images">
        /// List of image infos. 
        /// 
        /// This method will not sort this list. 
        /// All images in this collection will be used, regardless of size.
        /// </param>
        /// <param name="maxWidth">
        /// The sprite won't be wider than this.
        /// </param>
        /// <param name="maxHeight">
        /// The generated sprite won't be higher than this.
        /// </param>
        /// <param name="canvasStats">
        /// The statistics produced by the canvas. These numbers are since the last call to its SetCanvasDimensions method.
        /// </param>
        /// <param name="lowestFreeHeightDeficitTallestRightFlushedImage">
        /// The lowest free height deficit for the images up to and including the tallest rectangle whose right hand border sits furthest to the right
        /// of all images.
        /// 
        /// This is the minimum amount by which the height of the canvas needs to be increased to accommodate that rectangle.
        /// if the width of the canvas is decreased to one less than the width now taken by images.
        /// 
        /// Note that providing the additional height might get some other (not right flushed) image to be placed higher, thereby
        /// making room for the flushed right image.
        /// 
        /// This will be set to Int32.MaxValue if there was never any free height deficit.
        /// </param>
        /// <returns>
        /// The generated sprite.
        /// 
        /// null if not all the images could be placed within the size limitations.
        /// </returns>
        protected virtual Atlas MappingRestrictedBox(
            IOrderedEnumerable<ImageInfo> images, 
            int maxWidth, int maxHeight, CanvasStats canvasStats,
            out int lowestFreeHeightDeficitTallestRightFlushedImage)
        {
            lowestFreeHeightDeficitTallestRightFlushedImage = 0;
            _canvas.SetCanvasDimensions(maxWidth, maxHeight);

            Atlas spriteInfo = new Atlas();
            int heightHighestRightFlushedImage = 0;
            int furthestRightEdge = 0;

            foreach (ImageInfo image in images)
            {
                int xOffset;
                int yOffset;
                int lowestFreeHeightDeficit;
                if (!_canvas.AddRectangle(
                    image.Width, image.Height, 
                    out xOffset, out yOffset, 
                    out lowestFreeHeightDeficit))
                {
                    // Not enough room on the canvas to place the rectangle
                    spriteInfo = null;
                    break;
                }

                MappedImageInfo imageLocation = new MappedImageInfo(xOffset, yOffset, image);
                spriteInfo.AddMappedImage(imageLocation);

                // Update the lowestFreeHeightDeficitTallestRightFlushedImage
                int rightEdge = image.Width + xOffset;
                if ((rightEdge > furthestRightEdge) ||
                    ((rightEdge == furthestRightEdge) && (image.Height > heightHighestRightFlushedImage)))
                {
                    // The image is flushed the furthest right of all images, or it is flushed equally far to the right
                    // as the furthest flushed image but it is taller. 

                    lowestFreeHeightDeficitTallestRightFlushedImage = lowestFreeHeightDeficit;
                    heightHighestRightFlushedImage = image.Height;
                    furthestRightEdge = rightEdge;
                }
            }

            _canvas.GetStatistics(canvasStats);

            return spriteInfo;
        }

    }
}
