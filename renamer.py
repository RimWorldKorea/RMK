import os

MAX_INDEX = 100001
RKM_DATA_PATH = os.path.join(os.getcwd(), 'Data')


flag_array = 1 << MAX_INDEX
empty_room = 1


def set(idx: int, set_flag: bool = True):
    global flag_array
    if set_flag is True:
        flag_array |= 1 << idx
    else:
        flag_array &= ~(1 << idx)


def get(idx: int) -> bool:
    global flag_array
    return (flag_array & (1 << idx)) != 0


def get_empty_idx() -> int:
    global empty_room
    for i in range(empty_room, MAX_INDEX):
        if get(i) is False:
            empty_room = i
            return i
    return -1


for (root, dirs, files) in os.walk(RKM_DATA_PATH):
    for file_name in files:
        if '.xml' in file_name:
            if file_name[:-4].isdecimal():
                set(int(file_name[:-4]), True)

for (root, dirs, files) in os.walk(RKM_DATA_PATH):
    for file_name in files:
        if '.xml' in file_name and not file_name[:-4].isdecimal():
            src = os.path.join(root, file_name)
            dest = os.path.join(root, '{0:05d}.xml'.format(int(get_empty_idx())))
            print(f'{src} -> {os.path.basename(dest)}')
            os.rename(src, dest)
            set(get_empty_idx())

print('Done!')