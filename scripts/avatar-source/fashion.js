import * as THREE from 'three';
import { mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js';
export const looks = [
 ['Draped Noir','#24202b','Кожа · асимметричная юбка'],
 ['Shadow Tailor','#29262d','Деконструкция · широкие брюки'],
 ['Ritual','#24252b','Высокий воротник · платформы'],
 ['Acid Nomad','#30312c','Объёмный нейлон · acid green'],
 ['Violet Veil','#58304e','Фиолетовый · многослойность'],
 ['Night Mass','#24222c','Длинное пальто · цепи'],
 ['Silver Noir','#525260','Draped Noir · серебряная кожа'],
 ['Ash Ritual','#706a70','Ritual · пепельный хлопок'],
 ['Violet Nomad','#49334e','Acid Nomad · тёмная слива'],
 ['Midnight Tailor','#252b39','Shadow Tailor · чернильный']
];
export const lookNotes = [
 'Косая кожаная куртка, открытая талия, юбка с запахом и высокие платформы.',
 'Плечи оверсайз, многослойная рубашка, широкие брюки и тяжёлая обувь.',
 'Скульптурный воротник, вытянутый жилет, кольца и высокие ботинки.',
 'Объёмный бомбер, капюшон, карго и кислотные шнуры.',
 'Тонкий фиолетовый трикотаж, многослойная юбка, высокие ботинки и манжеты.',
 'Архитектурное пальто, перекрёстные ремни, свободные брюки и цепи.',
 'Серебряная версия асимметричного кожаного образа.',
 'Пепельная версия вытянутого силуэта с высоким воротником.',
 'Тёмно-фиолетовая версия рейв-карго с металлической фурнитурой.',
 'Чернильная версия деконструированного костюма.'
];
const v3=(x,y,z)=>new THREE.Vector3(x,y,z);
let fabricTexture;
function textile(){
 if(fabricTexture!==undefined)return fabricTexture;
 if(typeof document==='undefined')return null;
 const c=document.createElement('canvas');c.width=c.height=128;const ctx=c.getContext('2d');
 const data=ctx.createImageData(128,128);let seed=42;
 for(let y=0;y<128;y++)for(let x=0;x<128;x++){seed=(seed*1664525+1013904223)>>>0;const value=137+(seed%15)+(x%3===0?14:0)+(y%3===0?8:0);const k=(y*128+x)*4;data.data[k]=data.data[k+1]=data.data[k+2]=value;data.data[k+3]=255}
 ctx.putImageData(data,0,0);fabricTexture=new THREE.CanvasTexture(c);fabricTexture.wrapS=fabricTexture.wrapT=THREE.RepeatWrapping;fabricTexture.repeat.set(5,7);return fabricTexture;
}
function surface(fn,nu,nv){
 const p=[],uv=[],idx=[];
 for(let j=0;j<=nv;j++)for(let i=0;i<=nu;i++){p.push(...fn(i/nu,j/nv));uv.push(i/nu,j/nv)}
 for(let j=0;j<nv;j++)for(let i=0;i<nu;i++){let a=j*(nu+1)+i,b=a+1,c=a+nu+1,d=c+1;idx.push(a,c,b,b,c,d)}
 const g=new THREE.BufferGeometry();g.setAttribute('position',new THREE.Float32BufferAttribute(p,3));g.setAttribute('uv',new THREE.Float32BufferAttribute(uv,2));g.setIndex(idx);g.computeVertexNormals();return g;
}
function interpolate(rings,t){const n=(rings.length-1)*t,i=Math.min(rings.length-2,Math.floor(n)),f=n-i;return rings[i].map((x,j)=>THREE.MathUtils.lerp(x,rings[i+1][j]??0,f))}
function mesh(parent,g,mat){const m=new THREE.Mesh(g,mat);parent.add(m);return m}
function ellipsoid(parent,mat,x,y,z,rx,ry,rz,detail=16){const m=mesh(parent,new THREE.SphereGeometry(1,detail,Math.max(8,detail/2)),mat);m.position.set(x,y,z);m.scale.set(rx,ry,rz);return m}
function tube(parent,mat,points,r=.006,segments=20,sides=6){const curve=new THREE.CatmullRomCurve3(points.map(p=>v3(...p)));return mesh(parent,new THREE.TubeGeometry(curve,segments,r,sides,false),mat)}
function taper(parent,mat,points,radii,detail=24,sides=12){
 const c=new THREE.CatmullRomCurve3(points.map(p=>v3(...p))),frames=c.computeFrenetFrames(detail,false);
 return mesh(parent,surface((u,v)=>{const i=Math.min(detail,Math.round(v*detail)),p=c.getPointAt(v),q=interpolate(radii,v),a=u*Math.PI*2;return p.addScaledVector(frames.normals[i],Math.cos(a)*q[0]).addScaledVector(frames.binormals[i],Math.sin(a)*q[1]).toArray()},sides,detail),mat);
}
function ringsMesh(parent,mat,rings,segments=32,rows=28,fold=0,phase=0){
 return mesh(parent,surface((u,v)=>{const [y,rx,rz,cx=0,cz=0]=interpolate(rings,v),a=u*Math.PI*2,ripple=fold*Math.sin(a*13+v*7+phase)*Math.sin(Math.PI*v);return [cx+Math.sin(a)*(rx+ripple),y,cz+Math.cos(a)*(rz+ripple*.55)]},segments,rows),mat);
}
function ring(parent,mat,x,y,z,r=.018,axis='front'){const m=mesh(parent,new THREE.TorusGeometry(r,.0035,5,16),mat);m.position.set(x,y,z);if(axis==='flat')m.rotation.x=Math.PI/2;return m}
function panel(parent,mat,left,right,nu=8,nv=18,fold=.006){
 const a=new THREE.CatmullRomCurve3(left.map(p=>v3(...p))),b=new THREE.CatmullRomCurve3(right.map(p=>v3(...p)));
 return mesh(parent,surface((u,v)=>{const p=a.getPoint(v).lerp(b.getPoint(v),u);p.z+=Math.sin(u*Math.PI*6+v*5)*fold*Math.sin(u*Math.PI);return p.toArray()},nu,nv),mat);
}
function stitch(parent,mat,points,detail){return tube(parent,mat,points,.0014,detail?24:12,4)}
function strap(parent,mat,a,b,width=.02){const d=v3(...b).sub(v3(...a));const m=mesh(parent,new THREE.BoxGeometry(width,d.length(),.012),mat);m.position.copy(v3(...a).add(v3(...b)).multiplyScalar(.5));m.quaternion.setFromUnitVectors(v3(0,1,0),d.normalize());return m}
function chain(parent,mat,a,b,dip=.1,count=18){for(let i=0;i<count;i++){const t=i/(count-1),p=v3(...a).lerp(v3(...b),t);p.y-=Math.sin(t*Math.PI)*dip;const m=ring(parent,mat,p.x,p.y,p.z,.008);m.scale.y=1.3;m.rotation.y=i%2?1:0}}
function mergeParts(parent){
 const buckets=new Map();
 for(const child of [...parent.children]){if(!child.isMesh)continue;child.updateMatrix();const g=child.geometry.clone().applyMatrix4(child.matrix);const key=child.material;if(!buckets.has(key))buckets.set(key,[]);buckets.get(key).push(g);parent.remove(child);child.geometry.dispose()}
 for(const [mat,geometries] of buckets){const g=mergeGeometries(geometries,false);geometries.forEach(g=>g.dispose());if(g)mesh(parent,g,mat)}
}
export function createFashionAvatar(index=0,{detail=true}={}){
 index=((index%looks.length)+looks.length)%looks.length;const style=[0,1,2,3,4,5,0,2,3,1][index],female=[0,3,4].includes(style),root=new THREE.Group();
 const materials={};const make=(name,color,rough=.6,metal=0)=>materials[name]=new THREE.MeshStandardMaterial({name,color,roughness:rough,metalness:metal,side:THREE.DoubleSide});
 const skin=make('skin',female?'#bf968c':'#c3a294',.62,.04),lip=make('lip','#835b61',.5),shadow=make('shadow','#573e43',.7),white=make('eye','#b9b6ae',.35),iris=make('iris','#424c45',.26),pupil=make('pupil','#0b0b11',.26),hair=make('hair',style===2?'#b4adb3':style===5?'#817b83':'#16131c',.53,.08),strand=make('strand',style===2?'#d2c7cc':style===5?'#b6a8b5':'#302734',.48,.15),cloth=make('cloth',looks[index][1],style===0?.35:.77,style===0?.28:.04),inner=make('inner',style===4?'#302033':'#18171f',.78,.04),leather=make('leather','#242029',.3,.24),rubber=make('rubber','#16151c',.7),metal=make('metal','#a0a3ad',.25,.82),seam=make('seam',style===4?'#786075':'#49424e',.76),accent=make('accent',style===3?(index===8?'#b98be1':'#b4c75c'):'#88828d',.45,.3);
 const tex=textile();if(tex){cloth.bumpMap=tex;cloth.bumpScale=.002;inner.bumpMap=tex;inner.bumpScale=.0015}
 const D=detail?1:.65,S=detail?28:18;
 // Continuous, tapered torso and neck instead of primitive boxes.
 if(style===0||style===3)ringsMesh(root,skin,[[1.075,.185,.112],[1.18,.157,.1],[1.30,.155,.105]],S,18);
 if(style===5){mesh(root,surface((u,v)=>{const a=-.46+u*.92,r=interpolate([[1.08,.178,.115],[1.26,.145,.092],[1.43,.192,.12],[1.60,.22,.09],[1.67,.074,.062]],v);return [Math.sin(a)*r[1],r[0],Math.cos(a)*r[2]]},16,24),skin)}
 ringsMesh(root,skin,[[1.61,.16,.078],[1.67,.074,.062]],S,8);
 ringsMesh(root,skin,[[1.62,.073,.062],[1.73,.064,.062],[1.79,.065,.065]],20,10);
 // Head with jaw, cheekbones, brow and forehead contours.
 ringsMesh(root,skin,[[1.755,.025,.041,0,.017],[1.78,.066,.072,0,.015],[1.825,.094,.09],[1.87,.116,.106],[1.925,.116,.111],[1.975,.111,.108],[2.02,.092,.087],[2.054,.044,.04],[2.062,.003,.003]],detail?36:24,32);
 for(const sign of [-1,1]){
  ellipsoid(root,skin,sign*.118,1.887,-.004,.018,.037,.022,12);
  tube(root,shadow,[[sign*.122,1.91,.009],[sign*.13,1.89,.017],[sign*.121,1.868,.013]],.003,10,5);
  ellipsoid(root,skin,sign*.063,1.875,.087,.047,.033,.023,16);
  ellipsoid(root,white,sign*.049,1.929,.103,.026,.0095,.012,16);
  ellipsoid(root,iris,sign*.049,1.929,.114,.008,.008,.002,12);
  ellipsoid(root,pupil,sign*.049,1.929,.116,.0038,.0048,.001,10);
  ellipsoid(root,white,sign*.047,1.932,.117,.0017,.0017,.001,8);
  tube(root,shadow,[[sign*.024,1.928,.108],[sign*.045,1.939,.114],[sign*.072,1.93,.108]],.0025,14,5);
  tube(root,skin,[[sign*.024,1.926,.107],[sign*.047,1.919,.113],[sign*.073,1.928,.107]],.003,14,5);
  tube(root,hair,[[sign*.022,1.954,.107],[sign*.047,1.961,.114],[sign*.078,1.953,.104]],.0035,14,5);
  if(style!==3)ring(root,metal,sign*.125,1.861,.005,.012);
 }
 taper(root,skin,[[0,1.956,.106],[0,1.915,.12],[0,1.886,.139]],[[.009,.008],[.012,.013],[.016,.009]],14,12);
 for(const sign of [-1,1]){ellipsoid(root,skin,sign*.014,1.884,.123,.011,.008,.009,10);ellipsoid(root,shadow,sign*.011,1.88,.131,.004,.002,.002,8)}
 tube(root,lip,[[-.028,1.843,.098],[-.01,1.847,.108],[0,1.845,.111],[.01,1.847,.108],[.028,1.843,.098]],.0037,20,6);
 tube(root,lip,[[-.026,1.841,.099],[0,1.836,.109],[.026,1.841,.099]],.0039,18,6);
 tube(root,shadow,[[-.024,1.842,.105],[0,1.842,.113],[.024,1.842,.105]],.001,16,4);
 // Shaped scalp, separate locks and fine hair strands, not a helmet sphere.
 mesh(root,surface((u,v)=>{const a=u*Math.PI*2,theta=v*(1.47+.4*(1-Math.cos(a))*.5);return [Math.sin(theta)*Math.sin(a)*.128,1.94+Math.cos(theta)*.134,Math.sin(theta)*Math.cos(a)*.122-.012]},S,18),hair);
 const longHair=female&&style!==3,lockCount=detail?32:18;
 for(let i=0;i<lockCount;i++){
  const a=i/lockCount*Math.PI*2,front=Math.cos(a)>.4,side=Math.sin(a),back=Math.cos(a);
  const endY=front?1.965-(i%3)*.007:longHair?1.49+(i%5)*.035:1.79+(i%4)*.035;
  const points=[[side*.022,2.065,-.006+back*.018],[side*.10,2.028,back*.08],[side*.133,1.94,back*.124],[side*(longHair?.16:.135)+(front?.025:0),endY,back*.13]];
  tube(root,i%4===0?strand:hair,points,detail?.005:.008,detail?18:12,5);
  if(detail&&!front)tube(root,strand,points.map((p,j)=>[p[0]+.003,p[1]-.002,p[2]+.003]),.0009,20,4);
 }
 if(style===2||style===5)for(let i=0;i<10;i++)tube(root,i%3?hair:strand,[[-.065+i*.01,2.035,.04],[-.095+i*.012,1.99,.105],[-.1+i*.017,1.90-(i%3)*.025,.124]],.004,14,5);
 // Cloth limbs share anatomical joints, with separate palms and five fingers.
 const arms=[],legs=[];
 for(const sign of [-1,1]){
  const arm=new THREE.Group();arm.position.set(sign*.229,1.595,0);root.add(arm);arms.push(arm);
  if(style===2)taper(arm,skin,[[0,0,0],[sign*.055,-.16,0],[sign*.07,-.32,.018],[sign*.058,-.47,.015],[sign*.048,-.59,.024]],[[.067,.061],[.057,.052],[.043,.04],[.044,.037],[.028,.027]],detail?30:20,14);
  const sleeves=[0,1,3,4,5].includes(style);
  if(sleeves){const bulk=style===3?.055:style===1||style===5?.024:.009;const mat=style===4?cloth:style===0?leather:cloth;
   taper(arm,mat,[[0,.014,0],[sign*.062,-.14,0],[sign*.078,-.31,.015],[sign*.058,-.46,.02],[sign*.048,-.58,.024]],[[.073+bulk,.066+bulk],[.06+bulk,.057+bulk],[.046+bulk,.045+bulk],[.046+bulk*.7,.042+bulk*.7],[.033,.032]],detail?30:20,16);
   for(let j=0;j<(detail?8:4);j++){const y=-.2-j*.043,x=sign*(.06+(j%2)*.007);tube(arm,seam,[[x-.035,y,.04],[x,y-.008,.067+(style===3?.035:0)],[x+.037,y+.006,.04]],.002,12,5)}
   ringsMesh(arm,style===4?metal:inner,[[-.56,.038,.035,sign*.05,.024],[-.61,.034,.032,sign*.048,.024]],16,4);
  }
  ellipsoid(arm,skin,sign*.048,-.644,.025,.035,.055,.023,16);
  const fingerLengths=[.075,.091,.095,.078];
  for(let j=0;j<4;j++){let x=sign*.048+(j-1.5)*.016,y=-.678,len=fingerLengths[j];taper(arm,skin,[[x,y,.026],[x,y-len*.5,.032],[x-sign*.006,y-len,.023]],[[.008,.008],[.0075,.007],[.0055,.005]],detail?10:6,8);if(detail)ellipsoid(arm,style===4?leather:lip,x-sign*.005,y-len+.01,.028,.0048,.008,.0015,8)}
  taper(arm,skin,[[sign*.018,-.628,.028],[-sign*.005,-.661,.045],[-sign*.01,-.696,.044]],[[.012,.012],[.01,.01],[.007,.007]],10,8);
  if(style===0||style===4){ringsMesh(arm,metal,[[-.54,.039,.034,sign*.05,.024],[-.575,.038,.033,sign*.05,.024]],18,4);ring(arm,metal,sign*.048,-.69,.031,.009,'flat')}
  // Tapered legs and footwear in leg-local coordinates.
  const leg=new THREE.Group();leg.position.set(sign*.126,1.02,0);root.add(leg);legs.push(leg);
  if(style===0||style===4)taper(leg,skin,[[0,0,0],[sign*.005,-.10,0],[sign*.008,-.21,0]],[[.103,.096],[.096,.09],[.087,.082]],14,16);
  const wide=[1,3,5].includes(style);if(wide){const cargo=style===3;
   ringsMesh(leg,cloth,[[.08,.13,.115],[ -.16,cargo?.18:.145,.14],[-.44,cargo?.19:.15,.145],[-.70,cargo?.16:.153,.14],[-.88,cargo?.115:.165,.13]],S,detail?34:22,.012,sign);
   for(let j=0;j<(detail?9:4);j++){let y=-.20-j*.074;stitch(leg,seam,[[-.09,y,.09],[-.015,y-.018,.15],[.09,y+.011,.09]],detail)}
   if(cargo){for(const y of [-.28,-.57]){panel(leg,inner,[[sign*.12,y+.07,.12],[sign*.16,y,.14],[sign*.12,y-.08,.11]],[[sign*.025,y+.07,.16],[sign*.025,y,.18],[sign*.02,y-.08,.14]],5,8,.003);strap(leg,seam,[sign*.02,y+.04,.17],[sign*.12,y+.04,.15],.014)}tube(leg,accent,[[sign*.10,.0,.13],[sign*.14,-.32,.15],[sign*.11,-.64,.13]],.003,16,5)}
  }else{
   const bootTop=style===2?-.34:style===0||style===4?-.20:-.55;
   ringsMesh(leg,leather,[[bootTop,.079,.075],[-.45,.065,.068],[-.64,.065,.06],[-.86,.048,.055]],S,24,.003);
   for(let j=0;j<(detail?13:7);j++){let y=-.32-j*.035;if(y<bootTop){stitch(leg,seam,[[-.04,y,.061],[0,y-.006,.074],[.04,y+.003,.06]],detail);if(style===4)tube(leg,metal,[[-.023,y,.076],[.023,y-.018,.078]],.0016,4,4)}}
   if(style===2)ringsMesh(leg,leather,[[.015,.106,.1],[-.18,.095,.09],[-.35,.08,.078],[-.5,.061,.065]],S,18,.003);
  }
  // Foot last shaped from heel to rounded toe; sculpted stacked soles.
  const sneaker=style===3,w=sneaker?.118:.094,soleTop=sneaker?-.89:-.86,soleBottom=-.99;
  mesh(leg,surface((u,v)=>{const a=u*Math.PI*2,yy=THREE.MathUtils.lerp(soleBottom,soleTop,v),r=1+.035*Math.sin(v*Math.PI*12);return [Math.sin(a)*w*r,yy,.055+Math.cos(a)*(Math.cos(a)>0?.20:.105)]},32,8),rubber);
  ellipsoid(leg,leather,0,soleTop+.035,.055,w*.97,.075,.19,24);
  for(let j=0;j<(detail?4:2);j++){let y=soleBottom+.018+j*.024;const pts=[];for(let i=0;i<=32;i++){let a=i/32*Math.PI*2;pts.push([Math.sin(a)*w*1.01,y,.055+Math.cos(a)*(Math.cos(a)>0?.202:.107)])}tube(leg,j===2?seam:rubber,pts,.003,32,5)}
  if(detail)for(let j=0;j<6;j++){let z=.08+j*.018,y=soleTop+.096-j*.006;tube(leg,sneaker?seam:inner,[[-.035,y,z],[.035,y-.003,z+.013]],.002,4,4)}
  if(sneaker)for(let j=0;j<5;j++){const z=.01+j*.04;ellipsoid(leg,seam,sign*.105,-.94,z,.012,.018,.025,8)}
 }
 // Body garments with deliberately different silhouettes.
 if(style===0){
  ringsMesh(root,leather,[[1.29,.155,.105],[1.38,.185,.125],[1.51,.224,.12],[1.62,.237,.086]],S,22,.003);
  panel(root,leather,[[-.22,1.60,.045],[-.1,1.49,.15],[.08,1.32,.12]],[[.03,1.64,.081],[.17,1.46,.14],[.14,1.28,.09]],10,18,.006);
  strap(root,metal,[-.16,1.55,.125],[.105,1.315,.135],.004);ring(root,metal,-.14,1.535,.137,.013);
 }
 if(style===1){
  ringsMesh(root,inner,[[.89,.21,.13],[1.18,.18,.13],[1.46,.216,.128],[1.62,.21,.095]],S,24,.012);
  // Long irregular shirt tails.
  panel(root,inner,[[-.14,1.28,.145],[-.10,1.03,.16],[-.16,.85,.15]],[[.02,1.28,.148],[.06,1.04,.17],[.08,.91,.18]],10,18,.014);
  for(let i=0;i<5;i++)ellipsoid(root,metal,.018,1.48-i*.09,.146,.0035,.0035,.003,8);
 }
 if(style===2){
  mesh(root,surface((u,v)=>{const a=.25+u*(Math.PI*2-.5),rx=THREE.MathUtils.lerp(.23,.29,v),rz=THREE.MathUtils.lerp(.115,.2,v);return [Math.sin(a)*(rx+.012*Math.sin(a*8+v*4)),1.60-v*(.67+.27*(.5+.5*Math.sin(a*2))),Math.cos(a)*rz]},S,30),cloth);
  panel(root,cloth,[[-.20,1.59,.09],[-.17,1.2,.15],[-.27,.63,.16]],[[.15,1.59,.09],[.13,1.23,.17],[.04,.91,.21]],12,22,.008);
  ringsMesh(root,leather,[[1.62,.105,.09],[1.73,.117,.096],[1.81,.12,.09]],28,12,.002);
  for(const sign of [-1,1]){strap(root,leather,[sign*.19,1.61,.06],[sign*.21,1.26,.13],.025);for(let j=0;j<4;j++)ring(root,metal,sign*(.194+j*.006),1.57-j*.104,.114+j*.008,.016)}
 }
 if(style===3){
  ringsMesh(root,inner,[[1.24,.155,.103],[1.39,.18,.125],[1.53,.204,.105]],S,18,.003);
  // Bomber is open at the front so the cropped inner garment remains visible.
  mesh(root,surface((u,v)=>{const a=.4+u*(Math.PI*2-.8),rad=.25+.05*Math.sin(v*Math.PI),y=1.64-v*.46;return [Math.sin(a)*rad,y,Math.cos(a)*(.13+.05*Math.sin(v*Math.PI))]},S,24),cloth);
  ringsMesh(root,inner,[[1.16,.24,.145],[1.22,.26,.16]],S,5,.003);
  // Deep, open hood; its opening leaves the face unobstructed.
  mesh(root,surface((u,v)=>{const a=.5+u*(Math.PI*2-1),y=1.63+v*.48,rx=.205*Math.sin(.65+v*2.0),rz=.17;return [Math.sin(a)*rx,y,Math.cos(a)*rz-.035]},S,24),cloth);
  for(const sign of [-1,1]){tube(root,accent,[[sign*.10,1.77,.10],[sign*.16,1.55,.14],[sign*.14,1.28,.17]],.004,18,5);ellipsoid(root,metal,sign*.14,1.28,.17,.008,.015,.007,10)}
 }
 if(style===4){
  ringsMesh(root,cloth,[[1.12,.16,.11],[1.29,.15,.103],[1.44,.192,.135],[1.58,.217,.102],[1.66,.082,.072]],S,28,.004);
  // Fitted opaque base and fine layered seams define the sheer-knit idea.
  panel(root,inner,[[-.16,1.42,.102],[-.14,1.27,.115],[-.155,1.16,.119]],[[.16,1.42,.102],[.14,1.27,.115],[.155,1.16,.119]],12,16,.002);
  for(let i=0;i<7;i++){const y=1.18+i*.036;tube(root,seam,[[-.155,y,.078],[-.08,y-.008,.128],[.08,y+.002,.128],[.155,y+.013,.078]],.0016,18,4)}
 }
 if(style===5){
  for(let sign of [-1,1]){strap(root,leather,[sign*.18,1.59,.09],[-sign*.13,1.21,.13],.035);strap(root,leather,[sign*.14,1.42,.13],[-sign*.16,1.30,.14],.025)}
  ring(root,metal,0,1.4,.153,.021);
 }
 // Tailored jacket/coat: shoulder architecture, open front, folded lapels.
 if(style===1||style===5){const end=style===5?.22:.97;
  mesh(root,surface((u,v)=>{const a=.38+u*(Math.PI*2-.76),r=interpolate([[.27,.12],[.25,.135],[.24,.15],[style===5?.37:.26,style===5?.24:.16]],v);return [Math.sin(a)*(r[0]+.01*Math.sin(a*15+v*4)*v),1.65-v*(1.65-end)+.018*Math.sin(a*3)*v,Math.cos(a)*r[1]]},S,32),cloth);
  for(const sign of [-1,1]){
   panel(root,cloth,[[sign*.08,1.7,.055],[sign*.18,1.55,.11],[sign*.13,1.29,.16]],[[sign*.205,1.62,.09],[sign*.245,1.48,.065],[sign*.17,1.35,.15]],10,18,.002);
   stitch(root,seam,[[sign*.08,1.697,.06],[sign*.18,1.55,.115],[sign*.13,1.29,.167]],detail);
   panel(root,inner,[[sign*.19,1.22,.13],[sign*.20,1.13,.145]],[[sign*.11,1.23,.16],[sign*.12,1.14,.17]],4,4,0);
   ellipsoid(root,metal,sign*.14,1.24,.165,.007,.007,.003,10);
  }
 }
 // Wrapped asymmetric skirts with real folds and open slit.
 if(style===0||style===4){
  const skirtMat=style===4?cloth:inner;
  for(let layer=0;layer<(style===4?3:2);layer++){
   mesh(root,surface((u,v)=>{const a=.3+u*(Math.PI*2-.68),rx=.202+v*(.17+layer*.012),rz=.135+v*(.10+layer*.012),fold=Math.sin(a*(14+layer*2)+v*5)*(.003+v*.018),hem=.17+layer*.07+.10*Math.sin(a*1.7+layer*.8);return [Math.sin(a)*(rx+fold),1.08-v*(1.08-hem),Math.cos(a)*(rz+fold*.7)+layer*.004]},detail?44:28,detail?38:26),skirtMat);
  }
  panel(root,skirtMat,[[-.21,1.06,.08],[-.11,.92,.18],[-.19,.59,.22]],[[.19,1.09,.10],[.07,.86,.2],[.1,.35,.25]],14,24,.012);
  tube(root,seam,[[-.20,1.057,.091],[-.08,1.03,.155],[.12,1.07,.14],[.20,1.09,.09]],.003,20,5);
  strap(root,leather,[-.19,1.09,.11],[.18,1.09,.11],.035);ring(root,metal,.13,1.09,.154,.022);
 }
 // Belts, linked chains, necklaces and restrained jewelry.
 if(style!==2){ringsMesh(root,leather,[[1.045,.202,.136],[1.08,.198,.133]],S,4);ring(root,metal,0,1.063,.139,.019)}
 if(style===5||style===1){chain(root,metal,[-.14,1.07,.14],[.18,1.08,.14],.12,detail?25:14);if(style===5)chain(root,metal,[-.14,1.06,.15],[.22,1.05,.12],.19,detail?30:16)}
 if(style!==2){tube(root,metal,[[-.05,1.705,.05],[-.073,1.61,.10],[0,1.565,.125],[.073,1.61,.10],[.05,1.705,.05]],.0022,24,5);ring(root,metal,0,1.566,.13,.01)}
 // Merge stationary geometry by material and each articulated limb separately.
 mergeParts(root);for(const part of [...arms,...legs])mergeParts(part);
 root.userData={arms,legs,index,style,detail};return root;
}
export function createFirstPersonHands(color){
 const root=new THREE.Group(),skin=new THREE.MeshStandardMaterial({color:'#bd968c',roughness:.58,metalness:.03}),nail=new THREE.MeshStandardMaterial({color:'#30252e',roughness:.28}),metal=new THREE.MeshStandardMaterial({color,roughness:.26,metalness:.78});
 for(const sign of [-1,1]){
  const a=new THREE.Group();a.position.set(sign*.31,-.39,-.55);a.rotation.z=sign*.28;
  taper(a,skin,[[0,-.43,.27],[0,-.27,.15],[0,-.10,.045],[0,0,0]],[[.065,.06],[.057,.051],[.041,.036],[.035,.03]],24,18);
  ellipsoid(a,skin,0,.045,-.028,.045,.065,.027,20);
  for(let j=0;j<4;j++){const x=(j-1.5)*.021,len=[.086,.108,.1,.078][j];taper(a,skin,[[x,.082,-.03],[x,.082+len*.5,-.045],[x*.92,.082+len,-.063]],[[.011,.01],[.009,.009],[.007,.006]],12,10);ellipsoid(a,nail,x*.94,.075+len,-.068,.006,.012,.002,10)}
  taper(a,skin,[[-sign*.035,.025,-.025],[-sign*.067,.046,-.043],[-sign*.076,.09,-.048]],[[.014,.013],[.012,.011],[.008,.008]],14,10);
  ringsMesh(a,metal,[[-.085,.047,.041,0,.035],[-.055,.045,.039,0,.022]],24,5);
  for(let i=0;i<2;i++)ring(a,metal,(i-.5)*.022,.12,-.047,.012,'flat');
  mergeParts(a);root.add(a);
 }
 root.userData.braceletMaterials=[metal];return root;
}
