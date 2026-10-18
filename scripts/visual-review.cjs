const assertEnglish = require('./assert-english.cjs');
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const http = require('node:http');
const fs = require('node:fs/promises');
const path = require('node:path');
const root = path.resolve(__dirname,'..');
const mime={'.html':'text/html','.js':'text/javascript','.css':'text/css','.glb':'model/gltf-binary','.jpg':'image/jpeg','.png':'image/png','.mp3':'audio/mpeg','.json':'application/json'};
(async()=>{
await fs.mkdir(path.join(root,'artifacts/visual'),{recursive:true});
const server=http.createServer(async(req,res)=>{try{
  const target=path.resolve(root,'dist',decodeURIComponent(req.url.split('?')[0]).replace(/^\//,'')||'index.html');
  if(!target.startsWith(root+'/dist/')){res.writeHead(403).end();return}
  const bytes=await fs.readFile(target);res.writeHead(200,{'Content-Type':mime[path.extname(target)]||'application/octet-stream'});res.end(bytes);
}catch{res.writeHead(404).end()}});
await new Promise(r=>server.listen(4174,'127.0.0.1',r));
const browser=await chromium.launch({headless:true,executablePath:process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE || undefined,args:['--no-sandbox','--enable-unsafe-swiftshader','--use-gl=angle','--use-angle=swiftshader']});
try{
 const page=await browser.newPage({viewport:{width:1100,height:700},deviceScaleFactor:.75});
 const errors=[];page.on('pageerror',err=>errors.push(String(err)));
 await page.goto('http://127.0.0.1:4174/');await assertEnglish(page);
 await page.getByRole('button',{name:'ENTER THE NIGHT ↗',exact:true}).click();
 await page.getByRole('button',{name:'Luna',exact:true}).click();
 await page.getByRole('button',{name:'CHOOSE YOUR LOOK →',exact:true}).click();
 await page.getByText('DRAG TO ROTATE ↔',{exact:true}).waitFor({state:'visible',timeout:60000});
 await assertEnglish(page);
 const label=process.argv[2]||'final';
 await page.screenshot({timeout:60000,path:root+`/artifacts/visual/wardrobe-${label}.jpg`});
 await assertEnglish(page);
 await page.getByRole('button',{name:'FIND A PARTY →',exact:true}).click();
 await assertEnglish(page);await page.getByRole('button',{name:'COUNT ME IN ↗',exact:true}).click();
 await assertEnglish(page);await page.getByRole('button',{name:'ENTER THE CLUB ↗',exact:true}).click();
 // Wait for all local model requests and native asset construction to settle.
 await page.waitForLoadState('networkidle');
 await page.waitForTimeout(1200);
 await page.screenshot({timeout:60000,path:root+`/artifacts/visual/club-${label}-entrance.jpg`});
 await page.getByRole('button',{name:'Club map',exact:true}).click();
 await page.getByRole('button',{name:'◉ Dance floor',exact:true}).click();
 async function walk(code,frames){await page.keyboard.down(code);for(let i=0;i<frames;i++)await page.evaluate(()=>new Promise(resolve=>requestAnimationFrame(resolve)));await page.keyboard.up(code)}
 await walk('KeyA',16);await walk('KeyW',6);
 // Approach an original guest using the shipped keyboard controls.
 await page.getByRole('button',{name:'ACTIONS ⊕',exact:true}).click();
 await page.screenshot({timeout:60000,path:root+`/artifacts/visual/club-${label}-interaction.jpg`});
 await assertEnglish(page);
 await page.clock.install();await page.clock.pauseAt(new Date(Date.now()+500));
 await page.getByRole('button',{name:'Heart',exact:true}).click();
 await page.clock.runFor(32);

 await page.screenshot({timeout:60000,path:root+`/artifacts/visual/club-${label}-heart.jpg`});
 await page.clock.resume();
 assert.deepEqual(errors,[]);
 console.log(JSON.stringify({label,errors,url:page.url(),voice:await page.locator('#npc-voice').getAttribute('src')}));
}finally{await browser.close();await new Promise(r=>server.close(r));}
})().catch(e=>{console.error(e);process.exit(1)});
