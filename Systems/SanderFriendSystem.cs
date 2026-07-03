using Content.Shared.Mobs.Components;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Sander.Components;

namespace Sander.Systems;

public sealed class SanderFriendSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<GetVerbsEvent<Verb>>(AddFriendVerb);
    }

    private void AddFriendVerb(GetVerbsEvent<Verb> ev)
    {
        if (!HasComp<MobStateComponent>(ev.Target))
            return;

        Verb verb;
        if (HasComp<SanderFriendComponent>(ev.Target))
        {
            verb = new Verb
            {
                Text = "Unfriend",
                Act = () => RemComp<SanderFriendComponent>(ev.Target),
                ClientExclusive = true
            };
        }
        else
        {
            verb = new Verb
            {
                Text = "Friend",
                Act = () => AddComp(ev.Target, new SanderFriendComponent()),
                ClientExclusive = true
            };
        }
        ev.Verbs.Add(verb);
    }

    public bool IsFriend(EntityUid entity)
    {
        return HasComp<SanderFriendComponent>(entity);
    }
}