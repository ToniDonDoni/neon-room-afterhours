const assertEnglish = require('./assert-english.cjs');
const {chromium}=require('playwright');
const http=require('node:http'),fs=require('node:fs/promises'),path=require('node:path'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'..');
(async()=>{
await fs.mkdir(path.join(root,'artifacts/visual'),{recursive:true});
const types={'.html':'text/html','.js':'text/javascript','.css':'text/css','.glb':'model/gltf-binary','.jpg':'image/jpeg','.mp3':'audio/mpeg'};
const server=http.createServer(async(req,res)=>{try{const file=path.resolve(root,'dist',decodeURIComponent(req.url.split('?')[0]).slice(1)||'index.html');if(!file.startsWith(root+'/dist/')){res.writeHead(403).end();return}res.setHeader('Content-Type',types[path.extname(file)]||'application/octet-stream');res.end(await fs.readFile(file));}catch{res.writeHead(404).end()}});
await new Promise(r=>server.listen(4175,'127.0.0.1',r));
const browser=await chromium.launch({headless:true,executablePath:process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE||undefined,args:['--no-sandbox','--enable-unsafe-swiftshader','--use-gl=angle','--use-angle=swiftshader']});
try{
 const page=await browser.newPage({viewport:{width:390,height:844},deviceScaleFactor:1,isMobile:true,hasTouch:true});const errors=[];page.on('pageerror',e=>errors.push(String(e)));
 await page.goto('http://127.0.0.1:4175/');await assertEnglish(page);
 await page.getByRole('button',{name:'ENTER THE NIGHT ↗',exact:true}).click();await page.getByRole('button',{name:'Luna',exact:true}).click();await page.getByRole('button',{name:'CHOOSE YOUR LOOK →',exact:true}).click();
 await page.getByText('DRAG TO ROTATE ↔',{exact:true}).waitFor({timeout:60000});
 await assertEnglish(page);
 await page.getByRole('button',{name:'FIND A PARTY →',exact:true}).click();await assertEnglish(page);await page.getByRole('button',{name:'COUNT ME IN ↗',exact:true}).click();
 await page.getByRole('button',{name:/^▣ Phone/}).click();await assertEnglish(page);await page.getByRole('button',{name:'ENTER THE CLUB ↗',exact:true}).click();
 await page.waitForLoadState('networkidle');await page.waitForTimeout(1000);
 await page.getByRole('button',{name:'Settings',exact:true}).click();await page.locator('#quality').selectOption('low');await page.getByRole('button',{name:'Close',exact:true}).click();
 const move=await page.locator('#move-stick').boundingBox(),look=await page.locator('#look-stick').boundingBox();assert.ok(move&&look);
 assert.ok(move.x>=0&&move.x+move.width<=390);assert.ok(look.x>=0&&look.x+look.width<=390);
 const cdp=await page.context().newCDPSession(page),m={x:move.x+move.width/2,y:move.y+move.height/2,id:1},l={x:look.x+look.width/2,y:look.y+look.height/2,id:2};
 await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[m,l]});
 await cdp.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{...m,x:m.x-25,y:m.y-35},{...l,x:l.x+30}]});
 assert.equal(await page.locator('.joystick.active').count(),2,'Both independent touch sticks capture their own finger');
 await page.waitForTimeout(500);await cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});assert.equal(await page.locator('.joystick.active').count(),0);
 await page.getByRole('button',{name:'ACTIONS ⊕',exact:true}).click();
 await assertEnglish(page);
 await page.clock.install();await page.clock.pauseAt(new Date(Date.now()+500));await page.clock.runFor(32);
 await page.screenshot({path:root+'/artifacts/visual/mobile-final.jpg',timeout:60000});
 for(const action of ['talk','heart','wave','five','dance','drink']){const box=await page.locator(`[data-action="${action}"]`).boundingBox();assert.ok(box&&box.width>=44&&box.height>=44&&box.x>=0&&box.x+box.width<=390&&box.y>=0&&box.y+box.height<=844,`${action} is reachable`)}
 assert.deepEqual(errors,[]);console.log(JSON.stringify({mobile:'passed',viewport:'390x844',simultaneousTouch:true,actionsReachable:6,errors}));
}finally{await browser.close();await new Promise(r=>server.close(r));}
})().catch(e=>{console.error(e);process.exit(1)});
