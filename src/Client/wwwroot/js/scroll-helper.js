// Scrolls a pantry row into view after creation. Rows carry
// id="pantry-item-{id}" (see PantryItemsList, PantryUrgentList).
export function scrollToItem(id) {
    const el = document.getElementById(`pantry-item-${id}`);
    if (el) {
        el.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }
}
