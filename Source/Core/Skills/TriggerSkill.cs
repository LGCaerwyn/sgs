﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sanguosha.Core.Triggers;
using Sanguosha.Core.Cards;
using Sanguosha.Core.UI;
using Sanguosha.Core.Games;
using System.Diagnostics;
using Sanguosha.Core.Players;


namespace Sanguosha.Core.Skills
{
    public delegate void TriggerActionWithCardsAndPlayers(Player owner, GameEvent gameEvent, GameEventArgs args, List<Card> cards, List<Player> players);

    public abstract class TriggerSkill : PassiveSkill
    {
        public TriggerSkill()
        {
            Triggers = new Dictionary<GameEvent, Trigger>();
        }

        public void NotifySkillUse(List<Player> targets)
        {
            ActionLog log = new ActionLog();
            log.GameAction = GameAction.None;
            log.SkillAction = this;
            log.Source = Owner;
            log.Targets = targets;
            log.SpecialEffectHint = GenerateSpecialEffectHintIndex(Owner, targets);
            Games.Game.CurrentGame.NotificationProxy.NotifySkillUse(log);
        }

        protected virtual int GenerateSpecialEffectHintIndex(Player source, List<Player> targets)
        {
            return 0;
        }

        public void NotifySkillUse()
        {
            NotifySkillUse(new List<Player>());
        }

        protected bool AskForSkillUse()
        {
            int answer;
            return (Game.CurrentGame.UiProxies[Owner].AskForMultipleChoice(
                    new MultipleChoicePrompt(Prompt.SkillUseYewNoPrompt, this), Prompt.YesNoChoices, out answer)
                    && answer == 1);
        }

        protected bool AskForSkillUse(ICardUsageVerifier verifier, out List<Card> cards, out List<Player> players, Prompt prompt = null)
        {
            ISkill skill;
            if (prompt == null) prompt = new CardUsagePrompt(this.GetType().Name, this);
            var ret = Game.CurrentGame.UiProxies[Owner].AskForCardUsage(
                    prompt, verifier, out skill, out cards, out players);
            Trace.Assert(skill == null);
            return ret;
        }

        protected class AutoNotifyUsagePassiveSkillTrigger : Trigger
        {
            private TriggerActionWithCardsAndPlayers execute;

            public TriggerActionWithCardsAndPlayers Execute
            {
                get { return execute; }
                set { execute = value; }
            }

            public Prompt Prompt { get; set; }

            public AutoNotifyUsagePassiveSkillTrigger(TriggerSkill skill, TriggerPredicate canExecute, TriggerActionWithCardsAndPlayers execute, TriggerCondition condition, ICardUsageVerifier verifier) :
                this(skill, new RelayTrigger(canExecute, null, condition), execute, verifier)
            { }

            public AutoNotifyUsagePassiveSkillTrigger(TriggerSkill skill, TriggerActionWithCardsAndPlayers execute, TriggerCondition condition, ICardUsageVerifier verifier) :
                this(skill, new RelayTrigger(null, condition), execute, verifier)
            { }

            protected AutoNotifyUsagePassiveSkillTrigger(TriggerSkill skill, RelayTrigger innerTrigger, TriggerActionWithCardsAndPlayers execute, ICardUsageVerifier verifier)
            {
                AskForConfirmation = false;
                IsAutoNotify = true;
                Skill = skill;
                InnerTrigger = innerTrigger;
                Execute = execute;
                Verifier = verifier;
                base.Owner = InnerTrigger.Owner;
            }

            public bool IsAutoNotify { get; set; }
            public bool? AskForConfirmation { get; set; }
            private ICardUsageVerifier verifier;

            protected ICardUsageVerifier Verifier
            {
                get { return verifier; }
                set { verifier = value; }
            }

            public override Player Owner
            {
                get
                {
                    return base.Owner;
                }
                set
                {
                    base.Owner = value;
                    if (InnerTrigger.Owner != value)
                    {
                        InnerTrigger.Owner = value;
                    }
                }
            }

            public override void Run(GameEvent gameEvent, GameEventArgs eventArgs)
            {
                if (!InnerTrigger.CheckConditions(gameEvent, eventArgs))
                {
                    return;
                }
                if (InnerTrigger.CanExecute(Owner, gameEvent, eventArgs))
                {
                    List<Card> cards = null;
                    List<Player> players = null;
                    if (((AskForConfirmation == null && !Skill.IsEnforced && !Skill.IsAwakening) || (AskForConfirmation == true)) && !Skill.AskForSkillUse())
                    {
                        return;
                    }
                    if (!Skill.AskForSkillUse(verifier, out cards, out players, Prompt)) return;
                    if (IsAutoNotify)
                    {
                        Skill.NotifySkillUse(players);
                    }
                    Execute(Owner, gameEvent, eventArgs, cards, players);
                }
            }

            public TriggerSkill Skill
            {
                get;
                set;
            }

            public RelayTrigger InnerTrigger
            {
                get;
                set;
            }
        }

        protected class AutoNotifyPassiveSkillTrigger : Trigger
        {
            public AutoNotifyPassiveSkillTrigger(TriggerSkill skill, TriggerPredicate canExecute, TriggerAction execute, TriggerCondition condition) :
                this(skill, new RelayTrigger(canExecute, execute, condition))
            { }

            public AutoNotifyPassiveSkillTrigger(TriggerSkill skill, TriggerAction execute, TriggerCondition condition) :
                this(skill, new RelayTrigger(execute, condition))
            { }

            public AutoNotifyPassiveSkillTrigger(TriggerSkill skill, RelayTrigger innerTrigger)
            {
                AskForConfirmation = null;
                IsAutoNotify = true;
                Skill = skill;
                InnerTrigger = innerTrigger;
                base.Owner = InnerTrigger.Owner;
            }

            public bool IsAutoNotify { get; set; }
            public bool? AskForConfirmation { get; set; }

            public override Player Owner
            {
                get
                {
                    return base.Owner;
                }
                set
                {
                    base.Owner = value;
                    if (InnerTrigger.Owner != value)
                    {
                        InnerTrigger.Owner = value;
                    }
                }
            }

            public override void Run(GameEvent gameEvent, GameEventArgs eventArgs)
            {
                if (!InnerTrigger.CheckConditions(gameEvent, eventArgs))
                {
                    return;
                }
                if (InnerTrigger.CanExecute(Owner, gameEvent, eventArgs))
                {
                    if (((AskForConfirmation == null && !Skill.IsEnforced && !Skill.IsAwakening) || (AskForConfirmation == true)) && !Skill.AskForSkillUse())
                    {
                        return;
                    }
                    if (IsAutoNotify)
                    {
                        Skill.NotifySkillUse(new List<Player>());
                    }
                    InnerTrigger.Execute(Owner, gameEvent, eventArgs);
                }
            }

            public TriggerSkill Skill
            {
                get;
                set;
            }

            public RelayTrigger InnerTrigger
            {
                get;
                set;
            }
        }

        protected class LosingSkillTrigger : Trigger
        {
            class OnLosingSkill : Trigger
            {
                public override void Run(GameEvent gameEvent, GameEventArgs eventArgs)
                {
                    SkillSetChangedEventArgs args = eventArgs as SkillSetChangedEventArgs;
                    if (args.Source != Owner || !args.IsLosingSkill || !args.Skills.Contains(Skill))
                    {
                        return;
                    }
                    Execute(Owner, gameEvent, eventArgs);
                    Game.CurrentGame.UnregisterTrigger(GameEvent.PlayerSkillSetChanged, this);
                }

                public OnLosingSkill(Player p, ISkill skill, TriggerAction execute)
                {
                    Owner = p;
                    Skill = skill;
                    Execute = execute;
                    Priority = int.MinValue;
                }

                TriggerAction Execute;
                ISkill Skill;
            }

            public LosingSkillTrigger(TriggerSkill skill, TriggerAction execute)
            {
                Skill = skill;
                Execute = execute;
            }

            public override Player Owner
            {
                get
                {
                    return base.Owner;
                }
                set
                {
                    base.Owner = value;
                }
            }

            public override void Run(GameEvent gameEvent, GameEventArgs eventArgs)
            {
                if (eventArgs.Source != Owner)
                {
                    return;
                }
                if (gameEvent == GameEvent.PlayerSkillSetChanged)
                {
                    SkillSetChangedEventArgs args = eventArgs as SkillSetChangedEventArgs;
                    if (args.IsLosingSkill || !args.Skills.Contains(Skill))
                    {
                        return;
                    }
                }
                Game.CurrentGame.RegisterTrigger(GameEvent.PlayerSkillSetChanged, new OnLosingSkill(Owner, Skill, Execute));
            }

            public TriggerAction Execute
            {
                get;
                set;
            }

            public TriggerSkill Skill
            {
                get;
                set;
            }
        }

        private bool _isTriggerInstalled;

        protected override void InstallTriggers(Players.Player owner)
        {
            Trace.Assert(!_isTriggerInstalled,
                string.Format("Trigger already installed for skill {0}", this.GetType().FullName));
            foreach (var pair in Triggers)
            {
                pair.Value.Owner = owner;
                Game.CurrentGame.RegisterTrigger(pair.Key, pair.Value);
            }
            _isTriggerInstalled = true;
        }

        protected override void UninstallTriggers(Players.Player owner)
        {
            Trace.Assert(_isTriggerInstalled,
                string.Format("Trigger not installed yet for skill {0}", this.GetType().FullName));
            _isTriggerInstalled = false;
            foreach (var pair in Triggers)
            {
                pair.Value.Owner = null;
                Game.CurrentGame.UnregisterTrigger(pair.Key, pair.Value);
            }
        }

        protected IDictionary<GameEvent, Trigger> Triggers
        {
            get;
            private set;
        }

    }



    public abstract class ArmorTriggerSkill : TriggerSkill, IEquipmentSkill
    {
        public Equipment ParentEquipment { get; set; }
    }
}
