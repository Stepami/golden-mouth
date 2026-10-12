import { Button, Sheet } from '.'

export function SkillHelp({ onClose }: { onClose: () => void }) {
  return <Sheet onClose={onClose}>
    <h2 style={{ fontSize: 20, marginBottom: 12 }}>Как поднять навыки</h2>
    <div className="why"><span>🔥 Смелость</span><span>Делись опытом и отвечай по делу</span></div>
    <div className="why"><span>😂 Юмор</span><span>Находи смешное в событиях мира</span></div>
    <div className="why"><span>🎯 В тему</span><span>Пиши о героях и событиях своего мира</span></div>
    <p className="small muted" style={{ margin: '12px 0' }}>Опыт за пост дают первые 10 единиц дневного расхода энергии; ответы в треде тоже расходуют этот бюджет. Первые 3 единицы дают ×2 опыта, выполненное желание — ×1,3. Главный навык получает весь опыт, остальные — по 15%. Даже неудачный пост помогает расти.</p>
    <Button onClick={onClose}>Понятно</Button>
  </Sheet>
}
