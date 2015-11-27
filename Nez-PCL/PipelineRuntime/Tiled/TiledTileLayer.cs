﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez.Textures;


namespace Nez.Tiled
{
	public class TiledTileLayer : TiledLayer
	{
		public readonly TiledMap tilemap;
		public int width;
		public int height;
		public readonly TiledTile[] tiles;

		public int tileWidth
		{
			get { return tilemap.tileWidth; }
		}

		public int tileHeight
		{
			get { return tilemap.tileHeight; }
		}


		public TiledTileLayer( TiledMap map, string name, int width, int height, TiledTile[] data ) : base( name )
		{
			this.width = width;
			this.height = height;
			tiles = data;

			tilemap = map;
			tiles = populateTilePositions();
		}


		/// <summary>
		/// loops through the tiles and sets each tiles x/y value
		/// </summary>
		/// <returns>The tile positions.</returns>
		TiledTile[] populateTilePositions()
		{
			for( var y = 0; y < height; y++ )
			{
				for( var x = 0; x < width; x++ )
				{
					if( tiles[x + y * width] != null )
					{
						tiles[x + y * width].x = x;
						tiles[x + y * width].y = y;
					}
				}
			}

			return tiles;
		}


		public override void draw( SpriteBatch spriteBatch )
		{
			var renderOrderFunction = getRenderOrderFunction();
			foreach( var tile in renderOrderFunction() )
			{
				if( tile == null )
					continue;
				
				var region = tilemap.getTileRegion( tile.id );
				if( region != null )
					renderLayer( spriteBatch, tilemap, tile, region );
			}
		}


		public override void draw( SpriteBatch spriteBatch, Vector2 position, float layerDepth, Rectangle cameraClipBounds )
		{
			// offset it by the entity position since the tilemap will always expect positions in its own coordinate space
			cameraClipBounds.Location -= position.ToPoint();

			var minX = tilemap.worldPositionToTilePositionX( cameraClipBounds.Left );
			var minY = tilemap.worldPositionToTilePositionY( cameraClipBounds.Top );
			var maxX = tilemap.worldPositionToTilePositionX( cameraClipBounds.Right );
			var maxY = tilemap.worldPositionToTilePositionY( cameraClipBounds.Bottom );

			// loop through and draw all the non-culled tiles
			for( var y = minY; y <= maxY; y++ )
			{
				for( var x = minX; x <= maxX; x++ )
				{
					var tile = getTile( x, y );

					if( tile == null )
						continue;

					var tileRegion = tilemap.getTileRegion( tile.id );

					// not exactly sure why we need to compensate 1 pixel here. Could be a bug in MonoGame?
					var tx = tile.x * tilemap.tileWidth + (int)position.X;
					var ty = tile.y * tilemap.tileHeight + (int)position.Y;
					var rotation = 0f;

					var spriteEffects = SpriteEffects.None;
					if( tile.flippedHorizonally )
						spriteEffects |= SpriteEffects.FlipHorizontally;
					if( tile.flippedVertically )
						spriteEffects |= SpriteEffects.FlipVertically;
					if( tile.flippedDiagonally )
					{
						if( tile.flippedHorizonally && tile.flippedVertically )
						{
							spriteEffects ^= SpriteEffects.FlipVertically;
							rotation = MathHelper.PiOver2;
							tx += tilemap.tileWidth;
						}
						else if( tile.flippedHorizonally )
						{
							spriteEffects ^= SpriteEffects.FlipVertically;
							rotation = -MathHelper.PiOver2;
							ty += tilemap.tileHeight;
						}
						else if( tile.flippedVertically )
						{
							spriteEffects ^= SpriteEffects.FlipHorizontally;
							rotation = MathHelper.PiOver2;
							tx += tilemap.tileWidth;
						}
						else
						{
							spriteEffects ^= SpriteEffects.FlipHorizontally;
							rotation = -MathHelper.PiOver2;
							ty += tilemap.tileHeight;
						}
					}

					spriteBatch.Draw( tileRegion.texture2D, new Vector2( tx, ty ), null, tileRegion.sourceRect, Vector2.Zero, rotation, Vector2.One, Color.White, spriteEffects, layerDepth );
				}
			}
		}


		void renderLayer( SpriteBatch spriteBatch, TiledMap map, TiledTile tile, Subtexture region )
		{
			switch( map.orientation )
			{
				case TiledMapOrientation.Orthogonal:
					renderOrthogonal( spriteBatch, tile, region );
					break;
				case TiledMapOrientation.Isometric:
					renderIsometric( spriteBatch, tile, region );
					break;
				case TiledMapOrientation.Staggered:
					throw new NotImplementedException( "Staggered maps are currently not supported" );
			}
		}


		void renderOrthogonal( SpriteBatch spriteBatch, TiledTile tile, Subtexture region )
		{
			// not exactly sure why we need to compensate 1 pixel here. Could be a bug in MonoGame?
			var tx = tile.x * tilemap.tileWidth;
			var ty = tile.y * ( tilemap.tileHeight - 1 );

			spriteBatch.Draw( region.texture2D, new Rectangle( tx, ty, region.sourceRect.Width, region.sourceRect.Height ), region.sourceRect, Color.White );
			//spriteBatch.Draw( region, new Rectangle( tx, ty, region.bounds.Width, region.bounds.Height ), Color.White );
		}


		void renderIsometric( SpriteBatch spriteBatch, TiledTile tile, Subtexture region )
		{
			var tx = ( tile.x * ( tilemap.tileWidth / 2 ) ) - ( tile.y * ( tilemap.tileWidth / 2 ) )
                //Center
			         + ( tilemap.width * ( tilemap.tileWidth / 2 ) )
                //Compensate Bug?
			         - ( tilemap.tileWidth / 2 );
                
			var ty = ( tile.y * ( tilemap.tileHeight / 2 ) ) + ( tile.x * ( tilemap.tileHeight / 2 ) )
                //Compensate Bug?
			         - ( tilemap.tileWidth + tilemap.tileHeight );

			spriteBatch.Draw( region.texture2D, new Rectangle( tx, ty, region.sourceRect.Width, region.sourceRect.Height ), region.sourceRect, Color.White );
			//spriteBatch.Draw( region, new Rectangle( tx, ty, region.bounds.Width, region.bounds.Height ), Color.White );
		}


		public TiledTile getTile( int x, int y )
		{
			return tiles[x + y * width];
		}


		/// <summary>
		/// returns the bounds Rectangle of the passed in tile
		/// </summary>
		/// <returns>The bounds for tile.</returns>
		/// <param name="tile">Tile.</param>
		/// <param name="tilemap">Tilemap.</param>
		public static Rectangle getBoundsForTile( TiledTile tile, TiledMap tilemap )
		{
			return new Rectangle( tile.x * tilemap.tileWidth, tile.y * tilemap.tileHeight, tilemap.tileWidth, tilemap.tileHeight );
		}


		/// <summary>
		/// note that world position assumes that the Vector2 was normalized to be in the tilemaps coordinates. i.e. if the tilemap
		/// is not at 0,0 then the world position should be moved so that it takes into consideration the tilemap offset from 0,0.
		/// Example: if the tilemap is at 300,300 then the passed in value should be worldPos - (300,300)
		/// </summary>
		/// <returns>The tile at world position.</returns>
		/// <param name="pos">Position.</param>
		public TiledTile getTileAtWorldPosition( Vector2 pos )
		{
			return getTile( tilemap.worldPositionToTilePositionX( pos.X ), tilemap.worldPositionToTilePositionY( pos.Y ) );
		}


		/// <summary>
		/// Returns a list of rectangles in tile space, where any non-null tile is combined into bounding regions
		/// </summary>
		/// <param name="layer">Layer.</param>
		public List<Rectangle> getCollisionRectangles()
		{
			var checkedIndexes = new bool?[tiles.Length];
			var rectangles = new List<Rectangle>();
			var startCol = -1;
			var index = -1;

			for( var y = 0; y < tilemap.height; y++ )
			{
				for( var x = 0; x < tilemap.width; x++ )
				{
					index = y * tilemap.width + x;
					var tile = getTile( x, y );

					if( tile != null && ( checkedIndexes[index] == false || checkedIndexes[index] == null ) )
					{
						if( startCol < 0 )
							startCol = x;

						checkedIndexes[index] = true;
					}
					else if( tile == null || checkedIndexes[index] == true )
					{
						if( startCol >= 0 )
						{
							rectangles.Add( findBoundsRect( startCol, x, y, checkedIndexes ) );
							startCol = -1;
						}
					}
				} // end for x

				if( startCol >= 0 )
				{
					rectangles.Add( findBoundsRect( startCol, tilemap.width, y, checkedIndexes ) );
					startCol = -1;
				}
			}

			return rectangles;
		}


		/// <summary>
		/// Finds the largest bounding rect around tiles between startX and endX, starting at startY and going
		/// down as far as possible
		/// </summary>
		/// <returns>The bounds rect.</returns>
		/// <param name="layer">Layer.</param>
		/// <param name="startX">Start x.</param>
		/// <param name="endX">End x.</param>
		/// <param name="startY">Start y.</param>
		/// <param name="checkedIndexes">Checked indexes.</param>
		public Rectangle findBoundsRect( int startX, int endX, int startY, bool?[] checkedIndexes )
		{
			var index = -1;

			for( var y = startY + 1; y < tilemap.height; y++ )
			{
				for( var x = startX; x < endX; x++ )
				{
					index = y * tilemap.width + x;
					var tile = getTile( x, y );

					if( tile == null || checkedIndexes[index] == true )
					{
						// Set everything we've visited so far in this row to false again because it won't be included in the rectangle and should be checked again
						for( var _x = startX; _x < x; _x++ )
						{
							index = y * tilemap.width + _x;
							checkedIndexes[index] = false;
						}

						return new Rectangle( startX * tilemap.tileWidth, startY * tilemap.tileHeight, ( endX - startX ) * tilemap.tileWidth, ( y - startY ) * tilemap.tileHeight );
					}

					checkedIndexes[index] = true;
				}
			}

			return new Rectangle( startX * tilemap.tileWidth, startY * tilemap.tileHeight, ( endX - startX ) * tilemap.tileWidth, ( tilemap.height - startY ) * tilemap.tileHeight );
		}


		/// <summary>
		/// gets a List of all the TiledTiles that intersect the passed in Rectangle
		/// </summary>
		/// <returns>The tiles intersecting bounds.</returns>
		/// <param name="layer">Layer.</param>
		/// <param name="bounds">Bounds.</param>
		public List<TiledTile> getTilesIntersectingBounds( Rectangle bounds )
		{
			var minX = tilemap.worldPositionToTilePositionX( bounds.X );
			var minY = tilemap.worldPositionToTilePositionY( bounds.Y );
			var maxX = tilemap.worldPositionToTilePositionX( bounds.Right );
			var maxY = tilemap.worldPositionToTilePositionY( bounds.Bottom );

			var tilelist = new List<TiledTile>();

			for( var x = minX; x <= maxX; x++ )
			{
				for( var y = minY; y <= maxY; y++ )
				{
					var tile = getTile( x, y );
					if( tile != null )
						tilelist.Add( tile );
				}
			}

			return tilelist;
		}


		Func<IEnumerable<TiledTile>> getRenderOrderFunction()
		{
			switch ( tilemap.renderOrder )
			{
				case TiledRenderOrder.LeftDown:
					return getTilesLeftDown;
				case TiledRenderOrder.LeftUp:
					return getTilesLeftUp;
				case TiledRenderOrder.RightDown:
					return getTilesRightDown;
				case TiledRenderOrder.RightUp:
					return getTilesRightUp;
			}

			throw new NotSupportedException( string.Format( "{0} is not supported", tilemap.renderOrder ) );
		}


		IEnumerable<TiledTile> getTilesRightDown()
		{
			for( var y = 0; y < height; y++ )
			{
				for( var x = 0; x < width; x++ )
					yield return getTile( x, y );
			}
		}


		IEnumerable<TiledTile> getTilesRightUp()
		{
			for( var y = height - 1; y >= 0; y-- )
			{
				for( var x = 0; x < width; x++ )
					yield return getTile( x, y );
			}
		}


		IEnumerable<TiledTile> getTilesLeftDown()
		{
			for( var y = 0; y < height; y++ )
			{
				for( var x = width - 1; x >= 0; x-- )
					yield return getTile( x, y );
			}
		}


		IEnumerable<TiledTile> getTilesLeftUp()
		{
			for( var y = height - 1; y >= 0; y-- )
			{
				for( var x = width - 1; x >= 0; x-- )
					yield return getTile( x, y );
			}
		}
	
	}
}