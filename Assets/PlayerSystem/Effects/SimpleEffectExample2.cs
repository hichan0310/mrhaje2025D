using EntitySystem;
using EntitySystem.Events; // 필요시 사용
using PlayerSystem.Tiling;
using UnityEngine;

namespace PlayerSystem.Effects
{
    // Polyomino(일반 클래스) + ITriggerEffect
    public class SimpleEffectExample2 : Polyomino, ITriggerEffect
    {
        // NOTE: MonoBehaviour가 아니므로 인스펙터로 할당 불가.
        // 코드에서 주입하거나, Resources.Load 등을 사용하세요.
        public Example2FireBall fireball;

        private AtkTagSet atkTagSet = new AtkTagSet().Add(AtkTags.fireball);

        public SimpleEffectExample2() : base("SimpleEffectExample2") { }
        public SimpleEffectExample2(string name) : base(name) { }

        public void trigger(Entity entity, float power)
        {
            if (fireball == null || entity == null) return;

            // MonoBehaviour가 아니므로 Object.Instantiate 사용
            var f = UnityEngine.Object.Instantiate(fireball);

            // 위치/방향은 게임 규칙에 맞게 나중에 설정
            // 예: f.transform.position = entity.transform.position;

            var stat = entity.stat.calculate();
            var tag = new AtkTagSet(atkTagSet);
            var dmg = stat.calculateTrueDamage(tag, 100 * power);

            // 생성자의 실제 시그니처에 맞게 조정하세요.
            f.damage = new(dmg, Vector3.zero, entity, null, tag);
        }

        // 최소 1셀 모양 제공(배치 가능하게)
        protected override (int x, int y)[] BaseCells() => new[] { (0, 0) };
    }
}
