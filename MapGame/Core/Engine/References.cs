using MapGame.Core.Geographic;
using MapGame.MVVM.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MapGame.Core.Engine
{
    public readonly struct AreaReference
    {
        private readonly Area? _area;
        private readonly System.Windows.Media.Color? _color;
        private readonly string? _identifier;

        public static implicit operator AreaReference(Area area) => new(area);
        public static implicit operator AreaReference(System.Windows.Media.Color color) => new(color);
        public static implicit operator AreaReference(string identifier) => new(identifier);

        private AreaReference(Area area) { _area = area; _color = null; _identifier = null; }
        private AreaReference(System.Windows.Media.Color color) { _area = null; _color = color; _identifier = null; }
        private AreaReference(string identifier) { _area = null; _color = null; _identifier = identifier; }

        public CommandOutput<Area> Resolve()
        {
            if (_area != null)
                return new(true, _area);

            if (_color.HasValue)
            {
                var colorOutput = EngineCommands.GetAreaByColor(_color.Value);

                if (!colorOutput.Status)
                    return new(false, null!, colorOutput.Message);

                // colorOutput.Output is a PixelArea but no worries compiler will cast him into Area
                return new(true, colorOutput.Output);
            }

            if (_identifier != null)
            {
                return EngineCommands.GetAreaByIdentifier(_identifier);
            }

            return new(false, null!, "Invalid Area reference");
        }
    }

    public readonly struct RegionReference
    {
        private readonly Region? _region;
        private readonly int? _id;
        private readonly string? _identifier;

        public static implicit operator RegionReference(Region region) => new(region);
        public static implicit operator RegionReference(int id) => new(id);
        public static implicit operator RegionReference(string identifier) => new(identifier);

        private RegionReference(Region region) { _region = region; _id = null; _identifier = null; }
        private RegionReference(int id) { _region = null; _id = id; _identifier = null; }
        private RegionReference(string identifier) { _region = null; _id = null; _identifier = identifier; }

        public CommandOutput<Region> Resolve()
        {
            if (_region != null) return new(true, _region);
            if (_id.HasValue) return EngineCommands.GetRegionById(_id.Value);
            if (_identifier != null) return EngineCommands.GetRegionByIdentifier(_identifier);

            return new(false, null!, "Invalid Region reference");
        }
    }

    public readonly struct HistoricalRegionReference
    {
        private readonly HistoricalRegion? _hRegion;
        private readonly string? _identifier;

        public static implicit operator HistoricalRegionReference(HistoricalRegion hRegion) => new(hRegion);
        public static implicit operator HistoricalRegionReference(string identifier) => new(identifier);

        private HistoricalRegionReference(HistoricalRegion hRegion) { _hRegion = hRegion; _identifier = null; }
        private HistoricalRegionReference(string identifier) { _hRegion = null; _identifier = identifier; }
    }

    public readonly struct CountryReference
    {
        private readonly Country? _country;
        private readonly string? _identifier;

        public static implicit operator CountryReference(Country country) => new(country);
        public static implicit operator CountryReference(string identifier) => new(identifier);

        private CountryReference(Country country) { _country = country; _identifier = null; }
        private CountryReference(string identifier) { _country = null; _identifier = identifier; }

        public CommandOutput<Country> Resolve()
        {
            if (_country != null) return new(true, _country);
            if (_identifier != null) return EngineCommands.GetCountryByIdentifier(_identifier);

            return new(false, null!, "Invalid Country reference");
        }
    }
}
