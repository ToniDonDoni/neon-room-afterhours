const {chromium}=require('playwright');
const fs=require('node:fs/promises'),path=require('node:path');
const root=path.resolve(__dirname,'..');
(async()=>{
 const reference=process.argv[2];if(!reference)throw new Error('Usage: node scripts/compare-reference.cjs /path/to/reference.png');
 const img=async(file,type='image/jpeg')=>`data:${type};base64,${(await fs.readFile(file)).toString('base64')}`;
 const images=await Promise.all([img(reference,'image/png'),img(root+'/artifacts/visual/club-05-heart.jpg'),img(root+'/artifacts/visual/club-01-before.jpg'),img(root+'/artifacts/visual/club-05-entrance.jpg')]);
 const browser=await chromium.launch({headless:true,executablePath:process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE||undefined,args:['--no-sandbox']});
 try{const page=await browser.newPage({viewport:{width:1500,height:1110},deviceScaleFactor:1});
 await page.setContent(`<html><head><style>*{box-sizing:border-box}body{margin:0;padding:28px;background:#0d0a12;color:#ece5f0;font:15px Arial}h1{margin:0;font-size:28px;letter-spacing:2px}p{color:#b8aabd;margin:10px 0 20px}.grid{display:grid;grid-template-columns:1fr 1fr;gap:20px}.card{border:1px solid #59405c;border-radius:8px;overflow:hidden;background:#17121b}h2{font-size:13px;letter-spacing:1px;padding:14px 18px;margin:0;color:#efc8e7}img{display:block;width:100%;height:424px;object-fit:contain;background:#08060c}.note{font-size:13px;color:#c6b4ce;margin-top:18px;line-height:1.5}</style></head><body><h1>NEON ROOM / СРАВНЕНИЕ ГРАФИКИ</h1><p>Референс и реальные кадры PlayCanvas. Проверка локальным Playwright.</p><div class="grid">${images.map((src,i)=>`<div class="card"><h2>${['ТВОЙ РЕФЕРЕНС','ИГРА · ПОСЛЕ ПЯТИ ПРОХОДОВ','ПЕРВЫЙ КАДР · ПЕРЕСВЕТ','ПОСЛЕ · ТОТ ЖЕ ВИД ОТ ВХОДА'][i]}</h2><img src="${src}"></div>`).join('')}</div><div class="note">Приближены: розово-голубой свет, тёмные материалы, потолочные кольца, толпа, круговое меню, жест руками.<br>Отличия сохраняются: стилизованные персонажи и волосы, упрощённая кожа и отражения. Это ещё не фотореализм референса.</div></body></html>`);
 await page.screenshot({path:root+'/artifacts/visual/comparison.jpg',fullPage:true});
 }finally{await browser.close()}
})();
