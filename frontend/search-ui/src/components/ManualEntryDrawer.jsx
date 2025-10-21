import { useEffect } from 'react';
import PropTypes from 'prop-types';
import { Button, Drawer, Form, Input, Space, Typography } from 'antd';
import { MinusCircleOutlined, PlusOutlined } from '@ant-design/icons';

const createEmptyField = () => ({ key: '', value: '' });

export function ManualEntryDrawer({ open, onClose, onSubmit, loading, defaultDataset }) {
  const [form] = Form.useForm();

  useEffect(() => {
    if (open) {
      form.setFieldsValue({
        dataset: defaultDataset,
        fields: [createEmptyField()]
      });
    } else {
      form.resetFields();
    }
  }, [open, defaultDataset, form]);

  const handleFinish = (values) => {
    const fields = {};
    values.fields
      .filter((field) => field.key)
      .forEach((field) => {
        fields[field.key] = field.value;
      });
    onSubmit({ dataset: values.dataset, fields });
  };

  return (
    <Drawer
      title="Добавить запись"
      width={480}
      onClose={onClose}
      open={open}
      destroyOnClose
      extra={
        <Space>
          <Button onClick={onClose}>Отмена</Button>
          <Button type="primary" loading={loading} onClick={() => form.submit()}>
            Сохранить
          </Button>
        </Space>
      }
    >
      <Form form={form} layout="vertical" onFinish={handleFinish} autoComplete="off">
        <Form.Item
          label="Название датасета"
          name="dataset"
          rules={[{ required: true, message: 'Укажите название датасета' }]}
        >
          <Input placeholder="Например: contacts" />
        </Form.Item>

        <Typography.Title level={5}>Поля записи</Typography.Title>
        <Form.List name="fields" initialValue={[createEmptyField()]}>
          {(fields, { add, remove }) => (
            <Space direction="vertical" style={{ width: '100%' }}>
              {fields.map(({ key, name, ...restField }) => (
                <Space key={key} align="baseline" style={{ display: 'flex' }}>
                  <Form.Item
                    {...restField}
                    name={[name, 'key']}
                    rules={[{ required: true, message: 'Ключ обязателен' }]}
                  >
                    <Input placeholder="Поле" />
                  </Form.Item>
                  <Form.Item
                    {...restField}
                    name={[name, 'value']}
                    rules={[{ required: true, message: 'Значение обязательно' }]}
                  >
                    <Input placeholder="Значение" />
                  </Form.Item>
                  {fields.length > 1 && <MinusCircleOutlined onClick={() => remove(name)} />}
                </Space>
              ))}
              <Button type="dashed" onClick={() => add(createEmptyField())} block icon={<PlusOutlined />}>
                Добавить поле
              </Button>
            </Space>
          )}
        </Form.List>
      </Form>
    </Drawer>
  );
}

ManualEntryDrawer.propTypes = {
  open: PropTypes.bool.isRequired,
  onClose: PropTypes.func.isRequired,
  onSubmit: PropTypes.func.isRequired,
  loading: PropTypes.bool,
  defaultDataset: PropTypes.string
};

ManualEntryDrawer.defaultProps = {
  loading: false,
  defaultDataset: ''
};

export default ManualEntryDrawer;
