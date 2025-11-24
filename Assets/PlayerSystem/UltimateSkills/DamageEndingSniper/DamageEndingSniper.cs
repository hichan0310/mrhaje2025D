using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using EntitySystem;
using EntitySystem.BuffTypes;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using TMPro;
using UnityEngine;

namespace PlayerSystem.UltimateSkills.DamageEndingSniper
{
    public class DamageEndingSniper : Ultimate
    {
        private float cooldown;
        private Dictionary<Entity, DisplayStack> stack = new();
        private bool executing = false;
        [SerializeField] private DisplayStack displayStack;
        [SerializeField] private SnipeAim snipeAim;
        [SerializeField] private SnipeBullet snipeBullet;
        private SnipeAim snipeAimNow;


        public override void eventActive(EventArgs eventArgs)
        {
            
            if (eventArgs is DamageGiveEvent giveEvent)
            {
                if (giveEvent.attacker == this.player)
                {
                    if (!stack.ContainsKey(giveEvent.target))
                    {
                        var displayStack = Instantiate(this.displayStack);
                        stack.Add(giveEvent.target, displayStack);
                        displayStack.target = giveEvent.target;
                    }

                    if (giveEvent.atkTags.Contains(AtkTags.skillDamage))
                    {
                        
                        stack[giveEvent.target].stack += 3;
                    }
                    else if (giveEvent.atkTags.Contains(AtkTags.normalAttackDamage))
                    {
                        stack[giveEvent.target].stack += 2;
                    }
                    else
                    {
                        stack[giveEvent.target].stack += 1;
                    }
                }
            }
        }

        public override void update(float deltaTime, Entity entity)
        {
            cooldown -= deltaTime;
            if (cooldown < 0) cooldown = 0;

            if (executing && finished == 5)
            {
                executing = false;
                cooldown = 1;
                finished = -1;
                snipeAimNow.destroy();
                Invoke("reset", 0.02f);
            }
        }

        public override string skillName => "피해 결산 저격";

        public override string description =>
            "적에게 공격을 하면 공격에 따라서 스택을 최대 100스택까지 쌓는다. " +
            "스킬 시전 시 시간을 잠시 멈추고 주변의 자신과 같은 로봇을 해킹하여 총 5개의 타겟에게 저격 요청을 날릴 수 있다. " +
            "스택에 따라서 피해 계수가 증가되며 같은 적에게 여러 번의 저격을 가할 수도 있다. " +
            "스킬을 한번 더 시전하여 시간을 다시 흐르게 하고 저격이 날아간다. 이후 스택은 모두 제거된다. " +
            "이 공격은 확정 치명타로 적용된다.";


        public override float timeleft
        {
            get => cooldown;
            set => cooldown = value;
        }

        private int finished = -1;

        public void finishBullet()
        {
            finished++;
            // Debug.Log(finished);
        }

        public override void execute()
        {
            if (executing)
            {
                if (finished == -1)
                {
                    // Debug.Log("asdfasdfasdfasdf");
                    var result = snipeAimNow.snipePositions;
                    finished = 5 - result.Count;
                    var stat = this.player.stat.calculate();
                    for (int i = 0; i < result.Count; i++)
                    {
                        snipe(i, result[i], stat);
                    }

                    // Time.timeScale = 0.05f;
                    TimeScaler.Instance.changeTimeScale(32);
                }
            }
            else if (cooldown <= 0)
            {
                new UltimateExecuteEvent(this.player, 80).trigger();
                TimeScaler.Instance.changeTimeScale(1f/512);
                this.snipeAimNow = Instantiate(snipeAim);
                executing = true;
            }
        }

        public void reset()
        {
            // Time.timeScale = 1;
            TimeScaler.Instance.changeTimeScale(16);
            foreach (var display in stack.Values)
            {
                Destroy(display.gameObject);
            }

            stack.Clear();
        }

        private void snipe(int i, Vector3 position, IStat stat)
        {
            var b = Instantiate(snipeBullet);
            b.transform.position = new Vector3(Mathf.Cos(Mathf.PI / 6 * (i+1)), Mathf.Sin(Mathf.PI / 6 * (i+1)), 0) * 20;
            b.target = position;
            b.snipe = this;
            b.stat = stat;
            b.stack = stack;
        }
    }
}