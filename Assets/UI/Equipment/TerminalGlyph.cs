using UnityEngine;
using UnityEngine.UI;
namespace Frontend
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TerminalGlyph : MaskableGraphic
    {
        public int Symbol;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if(Symbol<0){Line(vh,new(-10,0),new(10,0));Line(vh,new(0,-10),new(0,10));return;}
            switch(Symbol%10){
                case 0: Path(vh,new(-20,0),new(-11,0),new(-6,12),new(0,-15),new(6,8),new(10,0),new(20,0));break;
                case 1: Ring(vh,12);Line(vh,new(-20,0),new(-7,0));Line(vh,new(7,0),new(20,0));Line(vh,new(0,-20),new(0,-7));Line(vh,new(0,7),new(0,20));break;
                case 2: Path(vh,new(-12,-12),new(12,-12),new(12,12),new(-12,12),new(-12,-12));for(int i=-8;i<=8;i+=8){Line(vh,new(i,12),new(i,20));Line(vh,new(i,-12),new(i,-20));}break;
                case 3: Path(vh,new(2,20),new(-13,-2),new(-1,-2),new(-3,-20),new(14,5),new(3,5),new(2,20));break;
                case 4: Path(vh,new(-18,-12),new(3,9),new(-9,9),new(3,9),new(3,-3));Line(vh,new(-6,-16),new(17,7));break;
                case 5: Path(vh,new(16,-13),new(16,9),new(-15,9),new(-5,18),new(-15,9),new(-5,0));break;
                case 6: Path(vh,new(-12,-17),new(12,-17),new(12,15),new(-12,15),new(-12,-17));Line(vh,new(-5,20),new(5,20));for(int i=-9;i<10;i+=8)Line(vh,new(-6,i),new(6,i));break;
                case 7: Path(vh,new(-19,8),new(-8,17),new(5,5),new(-5,-5),new(-19,8));Path(vh,new(-5,-5),new(8,-17),new(19,-8),new(5,5));break;
                case 8: for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Line(vh,new Vector2(Mathf.Cos(a),Mathf.Sin(a))*7,new Vector2(Mathf.Cos(a),Mathf.Sin(a))*20);}break;
                default: Ring(vh,16);Path(vh,new(-2,-8),new(-2,8),new(6,0),new(-10,0));break;
            }
        }
        private void Ring(VertexHelper vh,float radius){for(int i=0;i<24;i++){float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;Line(vh,new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius);}}
        private void Path(VertexHelper vh,params Vector2[] p){for(int i=1;i<p.Length;i++)Line(vh,p[i-1],p[i]);}
        private void Line(VertexHelper vh,Vector2 a,Vector2 b){float scale=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)/48f;var center=rectTransform.rect.center;a=a*scale+center;b=b*scale+center;var n=new Vector2(-(b-a).y,(b-a).x).normalized*scale;int j=vh.currentVertCount;vh.AddVert(a+n,color,Vector2.zero);vh.AddVert(b+n,color,Vector2.zero);vh.AddVert(b-n,color,Vector2.zero);vh.AddVert(a-n,color,Vector2.zero);vh.AddTriangle(j,j+1,j+2);vh.AddTriangle(j,j+2,j+3);}
    }
}
