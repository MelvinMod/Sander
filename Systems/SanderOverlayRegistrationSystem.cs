using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Sander.Overlays;

namespace Sander.Systems;

public sealed class SanderOverlayRegistrationSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    private SanderItemSearchOverlay? _searchOverlay;
    private SanderImplantOverlay? _implantOverlay;
    private SanderSyndicatePirateOverlay? _syndicatePirateOverlay;
    private SanderSoundSubtitleOverlay? _soundSubtitleOverlay;
    private SanderFootstepsOverlay? _footstepsOverlay;
    private SanderAimbotOverlay? _aimbotOverlay;

    public override void Initialize()
    {
        _searchOverlay = new SanderItemSearchOverlay();
        _overlayManager.AddOverlay(_searchOverlay);

        _implantOverlay = new SanderImplantOverlay();
        _overlayManager.AddOverlay(_implantOverlay);

        _syndicatePirateOverlay = new SanderSyndicatePirateOverlay();
        _overlayManager.AddOverlay(_syndicatePirateOverlay);

        _soundSubtitleOverlay = new SanderSoundSubtitleOverlay();
        _overlayManager.AddOverlay(_soundSubtitleOverlay);

        _footstepsOverlay = new SanderFootstepsOverlay();
        _overlayManager.AddOverlay(_footstepsOverlay);

        _aimbotOverlay = new SanderAimbotOverlay();
        _overlayManager.AddOverlay(_aimbotOverlay);
    }

    public override void Shutdown()
    {
        if (_searchOverlay != null)
        {
            _overlayManager.RemoveOverlay(_searchOverlay);
            _searchOverlay = null;
        }

        if (_implantOverlay != null)
        {
            _overlayManager.RemoveOverlay(_implantOverlay);
            _implantOverlay = null;
        }

        if (_syndicatePirateOverlay != null)
        {
            _overlayManager.RemoveOverlay(_syndicatePirateOverlay);
            _syndicatePirateOverlay = null;
        }

        if (_soundSubtitleOverlay != null)
        {
            _overlayManager.RemoveOverlay(_soundSubtitleOverlay);
            _soundSubtitleOverlay = null;
        }

        if (_footstepsOverlay != null)
        {
            _overlayManager.RemoveOverlay(_footstepsOverlay);
            _footstepsOverlay = null;
        }

        if (_aimbotOverlay != null)
        {
            _overlayManager.RemoveOverlay(_aimbotOverlay);
            _aimbotOverlay = null;
        }
    }
}

