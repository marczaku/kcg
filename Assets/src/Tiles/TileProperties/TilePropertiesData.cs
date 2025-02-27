﻿using Enums;

namespace TileProperties
{
    public struct TilePropertiesData
    {
        public string Name; //later use string pool
        public string Description;
        public int TileId;
        
        public TileDrawProperties TileDrawType; //enum, hint for how tile should be drawn
        public int SpriteId; //spriteId
        public int SpriteId2; //used for composited tiles, like ore

        public int[] Variants;
        public int VariantCount;

        public PlanetTileCollisionType TileCollisionType;
        public bool IsExplosive;

        //note: ore is composited, others are just normal

        public byte Durability; //max health of tile
        
        //In case after the first newly created tileproperty with already properties being set,
        //you might want to change it anytime by accessing the id of a tile
        public void SetDescription(string description)
        {
            Description = description;
        }
        public void SetDurability(byte durability)
        {
            Durability = durability;
        }
        public void SetCollisionType(PlanetTileCollisionType collisionType)
        {
            TileCollisionType = collisionType;
        }

        public bool IsSolid 
        {
            get
            {
                return TileCollisionType == PlanetTileCollisionType.TileCollisionTypeSolid;
            }
        }

        private TilePropertiesData(string name, string description, int tileId) : this()
        {
            Name = name;
            Description = description;
            TileId = tileId;

            Variants = new int[16];
            VariantCount = 0;
        }

        private TilePropertiesData(string name, string description, int tileId,
            TileDrawProperties tileDrawType, int spriteId, int spriteId2) : this(name, description, tileId)
        {
            TileDrawType = tileDrawType;
            SpriteId = spriteId;
            SpriteId2 = spriteId2;

            Variants = new int[16];
            VariantCount = 0;
        }

        public TilePropertiesData(string name, string description, int tileId,
            TileDrawProperties tileDrawType, int spriteId, int spriteId2,
            PlanetTileCollisionType tileCollisionType, byte durability,
             bool isExplosive = false)
            : this(name, description, tileId, tileDrawType, spriteId, spriteId2)
        {
            TileCollisionType = tileCollisionType;
            Durability = durability;
            IsExplosive = isExplosive;

            Variants = new int[16];
            VariantCount = 0;
        }
    }
}