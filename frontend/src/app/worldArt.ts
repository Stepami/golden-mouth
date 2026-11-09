const itSmallCover = '/assets/worlds/it-portrait-480-v8.jpg'
const itCover = '/assets/worlds/it-portrait-960-v8.jpg'
const eveningSmallCover = '/assets/worlds/evening-portrait-480-v8.jpg'
const eveningCover = '/assets/worlds/evening-portrait-960-v8.jpg'
const housesSmallCover = '/assets/worlds/houses-portrait-480-v8.jpg'
const housesCover = '/assets/worlds/houses-portrait-960-v8.jpg'
const sorcerersSmallCover = '/assets/worlds/sorcerers-portrait-480-v8.jpg'
const sorcerersCover = '/assets/worlds/sorcerers-portrait-960-v8.jpg'
export const worldArt: Record<string, { cover: string; smallCover: string; pitch: string; genre: string }> = {
  'b3a1a522-0263-4796-a197-57a8731fc291': { cover: eveningCover, smallCover: eveningSmallCover, genre: 'Тайны · Романтика', pitch: 'Исчезнувшие улицы, чужие воспоминания и знакомства после заката.' },
  'aec9494b-4118-4525-8458-e85d759c8dd9': { cover: sorcerersCover, smallCover: sorcerersSmallCover, genre: 'Магия · Дружба', pitch: 'Школа меняется каждую ночь. Только ваша общая лента помнит всё.' },
  '8c02a7fb-3b55-4eb0-b957-1b4869ed9834': { cover: housesCover, smallCover: housesSmallCover, genre: 'Интриги · Приключения', pitch: 'Великие дома, драконы и союзы, от которых зависит северное царство.' },
  '5720946a-ea7c-4a10-9c50-51ce3deefc53': { cover: itCover, smallCover: itSmallCover, genre: 'Стартапы · Ирония', pitch: 'Запускай проекты, спорь о технологиях и попадай в стартап-драмы.' },
}
