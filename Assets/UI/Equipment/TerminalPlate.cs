using UnityEngine;
using UnityEngine.UI;

namespace Frontend
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TerminalPlate : MaskableGraphic
    {
        public Color Border = new Color(.18f,.29f,.35f);
        public float Cut = 14;
        public void SetStyle(Color fill, Color border) { color=fill; Border=border; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;float c=Mathf.Min(Cut,r.height*.25f);
            Vector2[] p={new(r.xMin,r.yMin),new(r.xMax-c,r.yMin),new(r.xMax,r.yMin+c),new(r.xMax,r.yMax),new(r.xMin+c,r.yMax),new(r.xMin,r.yMax-c)};
            vh.AddVert(r.center,color,Vector2.zero);
            foreach(var v in p)vh.AddVert(v,color,Vector2.zero);
            for(int i=0;i<p.Length;i++)vh.AddTriangle(0,i+1,(i+1)%p.Length+1);
            for(int i=0;i<p.Length;i++){
                Vector2 a=p[i],b=p[(i+1)%p.Length],n=new Vector2(-(b-a).y,(b-a).x).normalized;
                int j=vh.currentVertCount;
                vh.AddVert(a,Border,Vector2.zero);vh.AddVert(b,Border,Vector2.zero);vh.AddVert(b+n,Border,Vector2.zero);vh.AddVert(a+n,Border,Vector2.zero);
                vh.AddTriangle(j,j+1,j+2);vh.AddTriangle(j,j+2,j+3);
            }
        }
    }
}
